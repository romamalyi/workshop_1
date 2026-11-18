import { Pipe, PipeTransform } from '@angular/core';
import { categoryLabel } from '../../activities/activity.model';

@Pipe({ name: 'categoryLabel', standalone: true })
export class CategoryLabelPipe implements PipeTransform {
  transform(value: string): string {
    return categoryLabel(value);
  }
}
