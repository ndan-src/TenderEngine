import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { NgChartsModule } from 'ng2-charts';
import { ChartConfiguration, ChartData } from 'chart.js';
import {
  Chart,
  BarController, BarElement, CategoryScale, LinearScale,
  LineController, LineElement, PointElement,
  Tooltip, Legend, Filler
} from 'chart.js';
import { SupabaseService, DateRangeFilter, LabelValue } from '../../services/supabase.service';

Chart.register(
  BarController, BarElement, CategoryScale, LinearScale,
  LineController, LineElement, PointElement,
  Tooltip, Legend, Filler
);

type Preset = 'week' | 'month' | 'quarter' | 'custom';

@Component({
  selector: 'app-trends-dashboard',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatCardModule, MatButtonToggleModule, MatFormFieldModule,
    MatInputModule, MatProgressSpinnerModule, MatIconModule,
    NgChartsModule
  ],
  templateUrl: './trends-dashboard.component.html',
  styleUrls: ['./trends-dashboard.component.scss']
})
export class TrendsDashboardComponent implements OnInit {
  loading = false;
  preset: Preset = 'month';
  groupBy: 'week' | 'month' = 'week';
  customFrom = '';
  customTo = '';

  // Chart data
  sectorSpendData?: ChartData<'bar'>;
  regionData?: ChartData<'bar'>;
  avgValueData?: ChartData<'bar'>;
  repeatBuyersData?: ChartData<'bar'>;
  spendOverTimeData?: ChartData<'line'>;

  // Shared chart options
  barOptions: ChartConfiguration['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: { legend: { display: false } },
    scales: {
      y: { beginAtZero: true, ticks: { callback: (v: string | number) => this.formatShort(Number(v)) } }
    }
  };

  timelineOptions: ChartConfiguration['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: { legend: { display: false } },
    scales: {
      y: { beginAtZero: true, ticks: { callback: (v: string | number) => this.formatShort(Number(v)) } }
    },
    elements: { line: { tension: 0.3 } }
  };

  constructor(private supabase: SupabaseService) {}

  ngOnInit() {
    this.load();
  }

  getRange(): DateRangeFilter {
    const to = new Date();
    let from = new Date();
    if (this.preset === 'week')    from.setDate(to.getDate() - 7);
    if (this.preset === 'month')   from.setMonth(to.getMonth() - 1);
    if (this.preset === 'quarter') from.setMonth(to.getMonth() - 3);
    if (this.preset === 'custom')  return { dateFrom: this.customFrom, dateTo: this.customTo };
    return {
      dateFrom: from.toISOString().split('T')[0],
      dateTo:   to.toISOString().split('T')[0]
    };
  }

  onPresetChange() {
    if (this.preset !== 'custom') this.load();
    this.groupBy = this.preset === 'week' ? 'week' : 'month';
  }

  applyCustomRange() {
    if (this.customFrom && this.customTo) this.load();
  }

  async load() {
    this.loading = true;
    const range = this.getRange();
    try {
      const [sector, region, avgVal, buyers, timeline] = await Promise.all([
        this.supabase.getSectorSpend(range),
        this.supabase.getRegionCounts(range),
        this.supabase.getAvgValueByCategory(range),
        this.supabase.getRepeatBuyers(range),
        this.supabase.getSpendOverTime(range, this.groupBy)
      ]);

      this.sectorSpendData  = this.toBarData(sector,  '#3f51b5');
      this.regionData       = this.toBarData(region,   '#009688');
      this.avgValueData     = this.toBarData(avgVal,   '#ff9800');
      this.repeatBuyersData = this.toBarData(buyers,   '#e91e63');
      this.spendOverTimeData = this.toLineData(timeline);
    } catch (e) {
      console.error('Failed to load trend data', e);
    } finally {
      this.loading = false;
    }
  }

  private toBarData(rows: LabelValue[], color: string): ChartData<'bar'> {
    return {
      labels: rows.map(r => r.label),
      datasets: [{ data: rows.map(r => r.value), backgroundColor: color + 'cc', borderColor: color, borderWidth: 1 }]
    };
  }

  private toLineData(rows: LabelValue[]): ChartData<'line'> {
    return {
      labels: rows.map(r => r.label),
      datasets: [{
        data: rows.map(r => r.value),
        borderColor: '#3f51b5',
        backgroundColor: '#3f51b520',
        fill: true,
        pointRadius: 4
      }]
    };
  }

  formatShort(n: number): string {
    if (n >= 1_000_000) return `€${(n / 1_000_000).toFixed(1)}M`;
    if (n >= 1_000)     return `€${(n / 1_000).toFixed(0)}K`;
    return `${n}`;
  }

  formatEuro(n: number): string {
    return new Intl.NumberFormat('en-GB', { style: 'currency', currency: 'EUR', maximumFractionDigits: 0 }).format(n);
  }
}

