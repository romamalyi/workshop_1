import { Component } from '@angular/core';

@Component({
  standalone: true,
  template: `
    <div class="tooltip">
      @if (heading) {
        <div class="heading">{{ heading }}</div>
      }
      <div>{{ text }}</div>
    </div>
  `,
  styles: [
    `
      .tooltip {
        background: #333;
        color: #fff;
        padding: 0.25rem 0.5rem;
        border-radius: 0.25rem;
        font-size: 0.75rem;
        white-space: pre;
      }

      .heading {
        margin-bottom: 0.125rem;
        font-size: 0.625rem;
        font-weight: 700;
        letter-spacing: 0.05em;
        text-transform: uppercase;
        color: #bbb;
      }
    `,
  ],
})
export class TooltipContentComponent {
  text = '';
  heading: string | null = null;
}
