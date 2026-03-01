import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatExpansionModule } from '@angular/material/expansion';
import { SupabaseService } from '../../services/supabase.service';
import { Tender } from '../../models/tender.model';

@Component({
  selector: 'app-tender-detail',
  standalone: true,
  imports: [
    CommonModule, RouterModule,
    MatButtonModule, MatIconModule, MatCardModule, MatChipsModule,
    MatDividerModule, MatProgressSpinnerModule, MatExpansionModule
  ],
  templateUrl: './tender-detail.component.html',
  styleUrls: ['./tender-detail.component.scss']
})
export class TenderDetailComponent implements OnInit {
  tender?: Tender;
  loading = true;
  error?: string;

  constructor(
    private route: ActivatedRoute,
    private supabase: SupabaseService
  ) {}

  async ngOnInit() {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    try {
      this.tender = await this.supabase.getTenderById(id) ?? undefined;
    } catch (e: any) {
      this.error = e.message;
    } finally {
      this.loading = false;
    }
  }

  formatValue(value?: number): string {
    if (value == null) return '—';
    return new Intl.NumberFormat('en-GB', { style: 'currency', currency: 'EUR', maximumFractionDigits: 0 }).format(value);
  }

  scoreClass(score?: number): string {
    if (score == null) return '';
    if (score >= 7) return 'score-high';
    if (score >= 4) return 'score-mid';
    return 'score-low';
  }

  statusLabel(status?: string | null): string {
    switch (status) {
      case 'Active':    return 'Active';
      case 'Amendment': return 'Amended Notice';
      case 'Awarded':   return 'Awarded';
      default:          return 'Unknown';
    }
  }

  statusClass(status?: string | null): string {
    switch (status) {
      case 'Active':    return 'status-active';
      case 'Amendment': return 'status-amendment';
      case 'Awarded':   return 'status-awarded';
      default:          return 'status-unknown';
    }
  }
}

