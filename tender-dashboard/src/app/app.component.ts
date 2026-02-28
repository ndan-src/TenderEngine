import { Component } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, MatToolbarModule, MatButtonModule, MatIconModule],
  template: `
    <mat-toolbar color="primary" class="app-toolbar">
      <mat-icon>gavel</mat-icon>
      <span class="brand">TenderEngine</span>
      <span class="spacer"></span>
      <a mat-button routerLink="/tenders" routerLinkActive="active-link">
        <mat-icon>list</mat-icon> Tenders
      </a>
      <a mat-button routerLink="/trends" routerLinkActive="active-link">
        <mat-icon>bar_chart</mat-icon> Trends
      </a>
    </mat-toolbar>
    <router-outlet />
  `,
  styles: [`
    .app-toolbar { gap: 10px; position: sticky; top: 0; z-index: 100; }
    .brand { font-size: 1.1rem; font-weight: 500; margin-left: 6px; }
    .spacer { flex: 1; }
    .active-link { background: rgba(255,255,255,0.15); border-radius: 4px; }
    a mat-icon { margin-right: 4px; font-size: 18px; vertical-align: middle; }
  `]
})
export class AppComponent {}

