import { NgModule } from '@angular/core';
import { SharedModule } from '../shared/shared.module';
import { AdminLayoutComponent } from './admin-layout.component';
import { AdminRoutingModule } from './admin-routing.module';

@NgModule({
  declarations: [AdminLayoutComponent],
  imports: [AdminRoutingModule, SharedModule],
})
export class AdminModule {}
