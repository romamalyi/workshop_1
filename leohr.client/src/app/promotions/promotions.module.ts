import { CommonModule } from '@angular/common';
import { ScrollingModule } from '@angular/cdk/scrolling';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { SharedModule } from '../shared/shared.module';
import { ActivityPickerModalComponent } from './components/activity-picker-modal/activity-picker-modal.component';
import { ActivitiesSectionComponent } from './components/activities-section/activities-section.component';
import { CelebrationVideoComponent } from './components/celebration-video/celebration-video.component';
import { CriteriaPanelComponent } from './components/criteria-panel/criteria-panel.component';
import { PromotionDetailComponent } from './components/promotion-detail/promotion-detail.component';
import { PromotionsLayoutComponent } from './components/promotions-layout/promotions-layout.component';
import { PromotionsPageComponent } from './components/promotions-page/promotions-page.component';
import { SendPromotionDialogComponent } from './components/send-promotion-dialog/send-promotion-dialog.component';
import { PromotionsRoutingModule } from './promotions-routing.module';
import { ActivityListComponent } from './components/activity-list/activity-list.component';
import { PendingApprovalsComponent } from './components/pending-approvals/pending-approvals.component';
import { PlannedActivitiesComponent } from './components/planned-activities/planned-activities.component';
import { PlannedDateDialogComponent } from './components/planned-date-dialog/planned-date-dialog.component';
import { AllLevelsModalComponent } from './components/all-levels-modal/all-levels-modal.component';
import { PromotionEditModalComponent } from './components/promotion-edit-modal/promotion-edit-modal.component';
import { PromotionsSettingsPageComponent } from './components/promotions-settings-page/promotions-settings-page.component';
import { PromotionHolderSettingsComponent } from './components/promotion-holder-settings/promotion-holder-settings.component';
import { KnowledgeEvaluationCardComponent } from './components/knowledge-evaluation-card/knowledge-evaluation-card.component';
import { KnowledgeEvaluationEditModalComponent } from './components/knowledge-evaluation-edit-modal/knowledge-evaluation-edit-modal.component';
import { KnowledgeEvaluationRequestModalComponent } from './components/knowledge-evaluation-request-modal/knowledge-evaluation-request-modal.component';
import { KnowledgeEvaluationsPageComponent } from './components/knowledge-evaluations-page/knowledge-evaluations-page.component';

@NgModule({
  declarations: [
    PromotionsLayoutComponent,
    PromotionsPageComponent,
    PromotionDetailComponent,
    PendingApprovalsComponent,
    PlannedActivitiesComponent,
    PlannedDateDialogComponent,
    CriteriaPanelComponent,
    ActivitiesSectionComponent,
    ActivityPickerModalComponent,
    AllLevelsModalComponent,
    PromotionEditModalComponent,
    ActivityListComponent,
    PromotionsSettingsPageComponent,
    PromotionHolderSettingsComponent,
    SendPromotionDialogComponent,
    KnowledgeEvaluationCardComponent,
    KnowledgeEvaluationEditModalComponent,
    KnowledgeEvaluationRequestModalComponent,
    KnowledgeEvaluationsPageComponent,
    CelebrationVideoComponent,
  ],
  imports: [CommonModule, ScrollingModule, FormsModule, RouterModule, PromotionsRoutingModule, SharedModule],
})
export class PromotionsModule {}
