import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AdminLayoutComponent } from './admin-layout.component';

const routes: Routes = [
  {
    path: '',
    component: AdminLayoutComponent,
    children: [
      { path: '', redirectTo: 'activities', pathMatch: 'full' },
      {
        path: 'activities',
        loadChildren: () => import('../activities/activities.module').then((m) => m.ActivitiesModule),
      },
      {
        path: 'promotion-criteria',
        loadChildren: () =>
          import('../promotion-criteria/promotion-criteria.module').then((m) => m.PromotionCriteriaModule),
      },
    ],
  },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class AdminRoutingModule {}
