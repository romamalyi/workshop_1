import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { KnowledgeEvaluationsPageComponent } from './components/knowledge-evaluations-page/knowledge-evaluations-page.component';
import { PendingApprovalsComponent } from './components/pending-approvals/pending-approvals.component';
import { PlannedActivitiesComponent } from './components/planned-activities/planned-activities.component';
import { PromotionDetailComponent } from './components/promotion-detail/promotion-detail.component';
import { PromotionsLayoutComponent } from './components/promotions-layout/promotions-layout.component';
import { PromotionsPageComponent } from './components/promotions-page/promotions-page.component';
import { PromotionsSettingsPageComponent } from './components/promotions-settings-page/promotions-settings-page.component';
import { promotionsAuthGuard } from '../core/guards/promotion-auth.guard';
import { featureEnabled } from '../core/guards/feature.guard';
import { canReadManage, canWriteManage } from '../core/guards/permission.guard';

const routes: Routes = [
  {
    path: '',
    component: PromotionsLayoutComponent,
    children: [
      { path: '', redirectTo: 'list', pathMatch: 'full' },
      { path: 'list', component: PromotionsPageComponent, canActivate: [promotionsAuthGuard] },
      { path: 'approvals', component: PendingApprovalsComponent, canActivate: [canReadManage] },
      { path: 'planned', component: PlannedActivitiesComponent, canActivate: [canReadManage] },
      {
        path: 'knowledge-evaluation',
        component: KnowledgeEvaluationsPageComponent,
        canActivate: [featureEnabled('knowledgeEvaluation'), canWriteManage],
      },
      { path: 'settings', component: PromotionsSettingsPageComponent, canActivate: [canWriteManage] },
      // A promotion id, or an employee's company ID for the link opened from the Employee app.
      { path: ':id', component: PromotionDetailComponent },
    ],
  },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class PromotionsRoutingModule {}
