import { Pipe, PipeTransform } from '@angular/core';
import { periodLabel } from '../../activities/activity.model';

@Pipe({ name: 'periodLabel', standalone: true })
export class PeriodLabelPipe implements PipeTransform {
  transform(value: string): string {
    return periodLabel(value);
  }
}
