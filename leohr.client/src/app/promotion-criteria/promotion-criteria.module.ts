import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SharedModule } from '../shared/shared.module';
import { PromotionCriteriaModalComponent } from './components/promotion-criteria-modal/promotion-criteria-modal.component';
import { PromotionCriteriaPageComponent } from './components/promotion-criteria-page/promotion-criteria-page.component';
import { PromotionCriteriaRoutingModule } from './promotion-criteria-routing.module';

@NgModule({
  declarations: [PromotionCriteriaPageComponent, PromotionCriteriaModalComponent],
  imports: [CommonModule, FormsModule, PromotionCriteriaRoutingModule, SharedModule],
})
export class PromotionCriteriaModule {}
