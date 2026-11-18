import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { PromotionCriteriaPageComponent } from './components/promotion-criteria-page/promotion-criteria-page.component';

const routes: Routes = [{ path: '', component: PromotionCriteriaPageComponent }];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class PromotionCriteriaRoutingModule {}
