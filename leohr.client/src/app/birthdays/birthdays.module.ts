import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { SharedModule } from '../shared/shared.module';
import { BirthdaysPageComponent } from './components/birthdays-page/birthdays-page.component';
import { BirthdaysRoutingModule } from './birthdays-routing.module';

@NgModule({
  declarations: [BirthdaysPageComponent],
  imports: [CommonModule, BirthdaysRoutingModule, SharedModule],
})
export class BirthdaysModule {}
