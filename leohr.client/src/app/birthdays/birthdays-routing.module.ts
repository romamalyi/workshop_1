import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { BirthdaysPageComponent } from './components/birthdays-page/birthdays-page.component';

const routes: Routes = [
  { path: '', component: BirthdaysPageComponent },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class BirthdaysRoutingModule {}
