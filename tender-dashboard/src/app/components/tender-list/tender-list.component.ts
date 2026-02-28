import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { SupabaseService, TenderFilter } from '../../services/supabase.service';
import { Tender } from '../../models/tender.model';

@Component({
  selector: 'app-tender-list',
  standalone: true,
  imports: [
    CommonModule, RouterModule, FormsModule,
    MatTableModule, MatPaginatorModule, MatSortModule,
    MatInputModule, MatFormFieldModule, MatButtonModule,
    MatIconModule, MatChipsModule, MatProgressSpinnerModule, MatTooltipModule
  ],
  templateUrl: './tender-list.component.html',
  styleUrls: ['./tender-list.component.scss']
})
export class TenderListComponent implements OnInit {
  tenders: Tender[] = [];
  totalCount = 0;
  loading = false;

  page = 0;
  pageSize = 25;
  sortColumn: keyof Tender = 'PublicationDate';
  sortAsc = false;

  filter: TenderFilter = {};
  searchText = '';
  dateFrom = '';
  dateTo = '';

  displayedColumns = [
    'TitleEn', 'BuyerNameEn', 'CpvCode', 'ValueEuro',
    'PublicationDate', 'SubmissionDeadline', 'SuitabilityScore', 'actions'
  ];

  constructor(private supabase: SupabaseService) {}

  ngOnInit() {
    this.load();
  }

  async load() {
    this.loading = true;
    try {
      const result = await this.supabase.getTenders(
        this.filter, this.page, this.pageSize, this.sortColumn, this.sortAsc
      );
      this.tenders = result.data;
      this.totalCount = result.count;
    } catch (e) {
      console.error('Failed to load tenders', e);
    } finally {
      this.loading = false;
    }
  }

  applyFilter() {
    this.filter = {
      search: this.searchText || undefined,
      dateFrom: this.dateFrom || undefined,
      dateTo: this.dateTo || undefined
    };
    this.page = 0;
    this.load();
  }

  clearFilter() {
    this.searchText = '';
    this.dateFrom = '';
    this.dateTo = '';
    this.filter = {};
    this.page = 0;
    this.load();
  }

  onPageChange(event: PageEvent) {
    this.page = event.pageIndex;
    this.pageSize = event.pageSize;
    this.load();
  }

  onSortChange(sort: Sort) {
    this.sortColumn = (sort.active as keyof Tender) || 'PublicationDate';
    this.sortAsc = sort.direction === 'asc';
    this.page = 0;
    this.load();
  }

  scoreClass(score?: number): string {
    if (score == null) return '';
    if (score >= 7) return 'score-high';
    if (score >= 4) return 'score-mid';
    return 'score-low';
  }

  formatValue(value?: number): string {
    if (value == null) return '—';
    return new Intl.NumberFormat('en-GB', { style: 'currency', currency: 'EUR', maximumFractionDigits: 0 }).format(value);
  }
}

