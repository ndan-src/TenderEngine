import { Injectable } from '@angular/core';
import { createClient, SupabaseClient } from '@supabase/supabase-js';
import { environment } from '../../environments/environment';
import { Tender } from '../models/tender.model';
import { nutsLabel } from '../data/nuts-codes';

export interface TenderFilter {
  search?: string;
  dateFrom?: string;
  dateTo?: string;
  minScore?: number;
  hasFatalFlaws?: boolean;
  noticeStatuses?: string[];   // e.g. ['Active', 'Amendment']
}

export interface PagedResult<T> {
  data: T[];
  count: number;
}

export interface DateRangeFilter {
  dateFrom: string;
  dateTo: string;
}

export interface LabelValue {
  label: string;
  value: number;
}

@Injectable({ providedIn: 'root' })
export class SupabaseService {
  private client: SupabaseClient;

  constructor() {
    this.client = createClient(environment.supabaseUrl, environment.supabaseKey);
  }

  async getTenders(
    filter: TenderFilter = {},
    page = 0,
    pageSize = 25,
    sortColumn: keyof Tender = 'PublicationDate',
    sortAsc = false
  ): Promise<PagedResult<Tender>> {
    const dateColumns: Array<keyof Tender> = ['PublicationDate', 'SubmissionDeadline', 'ContractStartDate', 'ContractEndDate', 'Deadline', 'CreatedAt'];
    const isDateColumn = dateColumns.includes(sortColumn);

    let query = this.client
      .from('Tenders')
      .select('*', { count: 'exact' })
      .order(sortColumn as string, {
        ascending: sortAsc,
        nullsFirst: isDateColumn ? false : true  // nulls last for dates so they don't pollute the top
      })
      .range(page * pageSize, (page + 1) * pageSize - 1);

    if (filter.search) {
      query = query.or(
        `TitleEn.ilike.%${filter.search}%,TitleDe.ilike.%${filter.search}%,BuyerNameEn.ilike.%${filter.search}%,BuyerName.ilike.%${filter.search}%`
      );
    }
    if (filter.dateFrom) {
      query = query.gte('PublicationDate', filter.dateFrom);
    }
    if (filter.dateTo) {
      query = query.lte('PublicationDate', filter.dateTo);
    }
    if (filter.minScore != null) {
      query = query.gte('SuitabilityScore', filter.minScore);
    }
    if (filter.hasFatalFlaws === true) {
      query = query.not('FatalFlaws', 'is', null);
    }
    if (filter.noticeStatuses && filter.noticeStatuses.length > 0) {
      query = query.in('NoticeStatus', filter.noticeStatuses);
    }

    const { data, error, count } = await query;
    if (error) throw error;
    return { data: (data as Tender[]) ?? [], count: count ?? 0 };
  }

  async getTenderById(id: number): Promise<Tender | null> {
    const { data, error } = await this.client
      .from('Tenders')
      .select('*')
      .eq('TenderID', id)
      .single();
    if (error) throw error;
    return data as Tender;
  }

  async getStats(): Promise<{
    total: number;
    withScore: number;
    avgScore: number;
    totalValue: number;
  }> {
    const { data, error } = await this.client
      .from('Tenders')
      .select('SuitabilityScore, ValueEuro');
    if (error) throw error;

    const rows = data as Pick<Tender, 'SuitabilityScore' | 'ValueEuro'>[];
    const withScore = rows.filter(r => r.SuitabilityScore != null);
    const avgScore =
      withScore.length > 0
        ? withScore.reduce((s, r) => s + (r.SuitabilityScore ?? 0), 0) / withScore.length
        : 0;
    const totalValue = rows.reduce((s, r) => s + (r.ValueEuro ?? 0), 0);

    return {
      total: rows.length,
      withScore: withScore.length,
      avgScore: Math.round(avgScore * 10) / 10,
      totalValue
    };
  }

  /** Sector spend: total ValueEuro grouped by CpvCode prefix (first 2 digits), sorted desc */
  async getSectorSpend(range: DateRangeFilter, topN = 10): Promise<LabelValue[]> {
    const { data, error } = await this.client
      .from('Tenders')
      .select('CpvCode, ValueEuro')
      .gte('PublicationDate', range.dateFrom)
      .lte('PublicationDate', range.dateTo)
      .not('CpvCode', 'is', null);
    if (error) throw error;

    const rows = data as Pick<Tender, 'CpvCode' | 'ValueEuro'>[];
    const map = new Map<string, number>();
    for (const r of rows) {
      const key = (r.CpvCode ?? '').substring(0, 2) || 'Unknown';
      map.set(key, (map.get(key) ?? 0) + (r.ValueEuro ?? 0));
    }
    return [...map.entries()]
      .map(([label, value]) => ({ label: `CPV ${label}`, value }))
      .sort((a, b) => b.value - a.value)
      .slice(0, topN);
  }

  /** Region activity: tender count grouped by NutsCode, sorted desc */
  async getRegionCounts(range: DateRangeFilter, topN = 10): Promise<LabelValue[]> {
    const { data, error } = await this.client
      .from('Tenders')
      .select('NutsCode, BuyerCity')
      .gte('PublicationDate', range.dateFrom)
      .lte('PublicationDate', range.dateTo);
    if (error) throw error;

    const rows = data as Pick<Tender, 'NutsCode' | 'BuyerCity'>[];
    const map = new Map<string, number>();
    for (const r of rows) {
      // Prefer NUTS label, fall back to BuyerCity, then Unknown
      const key = r.NutsCode
        ? nutsLabel(r.NutsCode)
        : (r.BuyerCity || 'Unknown');
      map.set(key, (map.get(key) ?? 0) + 1);
    }
    return [...map.entries()]
      .map(([label, value]) => ({ label, value }))
      .sort((a, b) => b.value - a.value)
      .slice(0, topN);
  }

  /** Average contract value by CPV sector */
  async getAvgValueByCategory(range: DateRangeFilter, topN = 10): Promise<LabelValue[]> {
    const { data, error } = await this.client
      .from('Tenders')
      .select('CpvCode, ValueEuro')
      .gte('PublicationDate', range.dateFrom)
      .lte('PublicationDate', range.dateTo)
      .not('ValueEuro', 'is', null)
      .not('CpvCode', 'is', null);
    if (error) throw error;

    const rows = data as Pick<Tender, 'CpvCode' | 'ValueEuro'>[];
    const map = new Map<string, { sum: number; count: number }>();
    for (const r of rows) {
      const key = (r.CpvCode ?? '').substring(0, 2) || 'Unknown';
      const existing = map.get(key) ?? { sum: 0, count: 0 };
      map.set(key, { sum: existing.sum + (r.ValueEuro ?? 0), count: existing.count + 1 });
    }
    return [...map.entries()]
      .map(([label, { sum, count }]) => ({ label: `CPV ${label}`, value: Math.round(sum / count) }))
      .sort((a, b) => b.value - a.value)
      .slice(0, topN);
  }

  /** Repeat contracting authorities: buyers with most tenders in the range */
  async getRepeatBuyers(range: DateRangeFilter, topN = 10): Promise<LabelValue[]> {
    const { data, error } = await this.client
      .from('Tenders')
      .select('BuyerNameEn, BuyerName')
      .gte('PublicationDate', range.dateFrom)
      .lte('PublicationDate', range.dateTo);
    if (error) throw error;

    const rows = data as Pick<Tender, 'BuyerNameEn' | 'BuyerName'>[];
    const map = new Map<string, number>();
    for (const r of rows) {
      const key = r.BuyerNameEn || r.BuyerName || 'Unknown';
      map.set(key, (map.get(key) ?? 0) + 1);
    }
    return [...map.entries()]
      .map(([label, value]) => ({ label, value }))
      .sort((a, b) => b.value - a.value)
      .filter(r => r.value > 1) // only repeat buyers
      .slice(0, topN);
  }

  /** Spend over time: total ValueEuro grouped by week or month */
  async getSpendOverTime(range: DateRangeFilter, groupBy: 'week' | 'month' = 'week'): Promise<LabelValue[]> {
    const { data, error } = await this.client
      .from('Tenders')
      .select('PublicationDate, ValueEuro')
      .gte('PublicationDate', range.dateFrom)
      .lte('PublicationDate', range.dateTo)
      .order('PublicationDate', { ascending: true });
    if (error) throw error;

    const rows = data as Pick<Tender, 'PublicationDate' | 'ValueEuro'>[];
    const map = new Map<string, number>();
    for (const r of rows) {
      if (!r.PublicationDate) continue;
      const d = new Date(r.PublicationDate);
      let key: string;
      if (groupBy === 'month') {
        key = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}`;
      } else {
        // ISO week
        const startOfYear = new Date(d.getFullYear(), 0, 1);
        const week = Math.ceil(((d.getTime() - startOfYear.getTime()) / 86400000 + startOfYear.getDay() + 1) / 7);
        key = `${d.getFullYear()}-W${String(week).padStart(2, '0')}`;
      }
      map.set(key, (map.get(key) ?? 0) + (r.ValueEuro ?? 0));
    }
    return [...map.entries()]
      .sort(([a], [b]) => a.localeCompare(b))
      .map(([label, value]) => ({ label, value }));
  }
}

