import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SharedModule } from '../shared/shared.module';
import { ActivityModalComponent } from './components/activity-modal/activity-modal.component';
import { ActivitiesPageComponent } from './components/activities-page/activities-page.component';
import { ActivitiesRoutingModule } from './activities-routing.module';

@NgModule({
  declarations: [ActivitiesPageComponent, ActivityModalComponent],
  imports: [CommonModule, FormsModule, ActivitiesRoutingModule, SharedModule],
})
export class ActivitiesModule {}
