import { OverlayModule } from '@angular/cdk/overlay';
import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActionIconComponent } from './components/action-icon/action-icon.component';
import { ApproveActivityDialogComponent } from './components/approve-activity-dialog/approve-activity-dialog.component';
import { AvatarComponent } from './components/avatar/avatar.component';
import { BadgeComponent } from './components/badge/badge.component';
import { ConfirmDialogComponent } from './components/confirm-dialog/confirm-dialog.component';
import { DatePickerComponent } from './components/date-picker/date-picker.component';
import { EmailTemplateCardComponent } from './components/email-template-editor/email-template-card.component';
import { EmailTemplateEditorComponent } from './components/email-template-editor/email-template-editor.component';
import { MultiSelectFilterComponent } from './components/multi-select-filter/multi-select-filter.component';
import { SearchSelectComponent } from './components/search-select/search-select.component';
import { ProgressBarComponent } from './components/progress-bar/progress-bar.component';
import { CategoryLabelPipe } from './pipes/category-label.pipe';
import { PeriodLabelPipe } from './pipes/period-label.pipe';
import { TechnologyLabelPipe } from './pipes/technology-label.pipe';
import { DialogDirective } from './directives/dialog.directive';
import { FitToWindowDirective } from './directives/fit-to-window.directive';
import { InViewDirective } from './directives/in-view.directive';
import { TooltipDirective } from './directives/tooltip.directive';

@NgModule({
  declarations: [
    AvatarComponent,
    BadgeComponent,
    EmailTemplateCardComponent,
    EmailTemplateEditorComponent,
    MultiSelectFilterComponent,
    SearchSelectComponent,
    ProgressBarComponent,
  ],
  imports: [
    CommonModule,
    FormsModule,
    OverlayModule,
    ActionIconComponent,
    ApproveActivityDialogComponent,
    ConfirmDialogComponent,
    DatePickerComponent,
    TechnologyLabelPipe,
    CategoryLabelPipe,
    PeriodLabelPipe,
    TooltipDirective,
    InViewDirective,
    DialogDirective,
    FitToWindowDirective,
  ],
  exports: [
    ActionIconComponent,
    ApproveActivityDialogComponent,
    AvatarComponent,
    BadgeComponent,
    MultiSelectFilterComponent,
    SearchSelectComponent,
    ProgressBarComponent,
    ConfirmDialogComponent,
    DatePickerComponent,
    EmailTemplateEditorComponent,
    TechnologyLabelPipe,
    CategoryLabelPipe,
    PeriodLabelPipe,
    TooltipDirective,
    InViewDirective,
    DialogDirective,
    FitToWindowDirective,
  ],
})
export class SharedModule {}
