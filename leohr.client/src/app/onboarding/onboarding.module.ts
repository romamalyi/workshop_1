import { OverlayModule } from '@angular/cdk/overlay';
import { DatePipe, NgTemplateOutlet } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SharedModule } from '../shared/shared.module';
import { OnboardingLayoutComponent } from './components/onboarding-layout/onboarding-layout.component';
import { OnboardingPageComponent } from './components/onboarding-page/onboarding-page.component';
import { OnboardingFlowWizardComponent } from './components/onboarding-flow-wizard/onboarding-flow-wizard.component';
import { OnboardingTemplateEditorComponent } from './components/onboarding-template-editor/onboarding-template-editor.component';
import { OnboardingTemplatesPageComponent } from './components/onboarding-templates-page/onboarding-templates-page.component';
import { OnboardingTemplatePageComponent } from './components/onboarding-template-page/onboarding-template-page.component';
import { OnboardingSettingsPageComponent } from './components/onboarding-settings-page/onboarding-settings-page.component';
import { OnboardingTypeAddTemplatesComponent } from './components/onboarding-type-add-templates/onboarding-type-add-templates.component';
import { OnboardingTypePageComponent } from './components/onboarding-type-page/onboarding-type-page.component';
import { OnboardingTypesPageComponent } from './components/onboarding-types-page/onboarding-types-page.component';
import { OnboardingRemindersSettingsComponent } from './components/onboarding-reminders-settings/onboarding-reminders-settings.component';
import { ChangeResponsibleDialogComponent } from './components/change-responsible-dialog/change-responsible-dialog.component';
import { OnboardingResponsiblePositionsSettingsComponent } from './components/onboarding-responsible-positions-settings/onboarding-responsible-positions-settings.component';
import { OnboardingPhasesSettingsComponent } from './components/onboarding-phases-settings/onboarding-phases-settings.component';
import { OnboardingFlowOverviewComponent } from './components/onboarding-flow-overview/onboarding-flow-overview.component';
import { NeedsAttentionPageComponent } from './components/needs-attention-page/needs-attention-page.component';
import { TaskAssigneeSelectComponent } from './components/task-assignee-select/task-assignee-select.component';
import { OnboardingTaskDetailComponent } from './components/onboarding-task-detail/onboarding-task-detail.component';
import { MyTasksPageComponent } from './components/my-tasks-page/my-tasks-page.component';
import { TaskCheckComponent } from './components/task-check/task-check.component';
import { RecheckMappingsDialogComponent } from './components/recheck-mappings-dialog/recheck-mappings-dialog.component';
import { OnboardingRoutingModule } from './onboarding-routing.module';

@NgModule({
  declarations: [
    OnboardingLayoutComponent,
    OnboardingPageComponent,
    OnboardingFlowWizardComponent,
    OnboardingTemplatesPageComponent,
    OnboardingTemplateEditorComponent,
    OnboardingTemplatePageComponent,
    OnboardingSettingsPageComponent,
    OnboardingTypesPageComponent,
    OnboardingTypePageComponent,
    OnboardingTypeAddTemplatesComponent,
    OnboardingRemindersSettingsComponent,
    OnboardingPhasesSettingsComponent,
    OnboardingResponsiblePositionsSettingsComponent,
    ChangeResponsibleDialogComponent,
    OnboardingFlowOverviewComponent,
    NeedsAttentionPageComponent,
    TaskAssigneeSelectComponent,
    MyTasksPageComponent,
    TaskCheckComponent,
    OnboardingTaskDetailComponent,
    RecheckMappingsDialogComponent,
  ],
  imports: [OnboardingRoutingModule, SharedModule, FormsModule, NgTemplateOutlet, DatePipe, OverlayModule],
  providers: [DatePipe],
})
export class OnboardingModule {}
