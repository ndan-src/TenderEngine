import { Routes } from '@angular/router';
import { TenderListComponent } from './components/tender-list/tender-list.component';
import { TenderDetailComponent } from './components/tender-detail/tender-detail.component';
import { TrendsDashboardComponent } from './components/trends-dashboard/trends-dashboard.component';

export const routes: Routes = [
  { path: '', redirectTo: 'tenders', pathMatch: 'full' },
  { path: 'tenders', component: TenderListComponent },
  { path: 'tenders/:id', component: TenderDetailComponent },
  { path: 'trends', component: TrendsDashboardComponent },
];

