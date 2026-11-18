import { Pipe, PipeTransform } from '@angular/core';
import { TECHNOLOGY_LABELS } from '../../promotion-criteria/promotion-criteria.model';

@Pipe({ name: 'technologyLabel', standalone: true })
export class TechnologyLabelPipe implements PipeTransform {
  transform(value: string): string {
    return TECHNOLOGY_LABELS[value] ?? value;
  }
}
