import { Injectable } from '@angular/core';
import { createClient, SupabaseClient } from '@supabase/supabase-js';
import { environment } from '../../environments/environment';
import { Tender } from '../models/tender.model';

export interface TenderFilter {
  search?: string;
  dateFrom?: string;
  dateTo?: string;
  minScore?: number;
  hasFatalFlaws?: boolean;
}

export interface PagedResult<T> {
  data: T[];
  count: number;
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
    let query = this.client
      .from('Tenders')
      .select('*', { count: 'exact' })
      .order(sortColumn as string, { ascending: sortAsc })
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
}

