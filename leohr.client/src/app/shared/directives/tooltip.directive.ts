import { Directive, ElementRef, HostListener, inject, input, OnDestroy } from '@angular/core';
import { Overlay, OverlayRef } from '@angular/cdk/overlay';
import { ComponentPortal } from '@angular/cdk/portal';
import { TooltipContentComponent } from '../components/tooltip/tooltip.component';

@Directive({
  selector: '[tooltip]',
  standalone: true,
})
export class TooltipDirective implements OnDestroy {
  tooltip = input.required<string | null>();
  tooltipHeading = input<string | null>(null);

  private overlayRef: OverlayRef | null = null;
  private overlay = inject(Overlay);
  private elementRef = inject(ElementRef);

  @HostListener('mouseenter')
  show() {
    const text = this.tooltip();
    if (!text) {
      return;
    }

    this.hide();
    this.overlayRef = this.overlay.create({
      positionStrategy: this.overlay
        .position()
        .flexibleConnectedTo(this.elementRef)
        .withPositions([
          { originX: 'center', originY: 'top', overlayX: 'center', overlayY: 'bottom', offsetY: -8 },
          { originX: 'center', originY: 'bottom', overlayX: 'center', overlayY: 'top', offsetY: 8 },
        ]),
      scrollStrategy: this.overlay.scrollStrategies.close(),
    });
    const portal = new ComponentPortal(TooltipContentComponent);
    const ref = this.overlayRef.attach(portal);
    ref.instance.text = text;
    ref.instance.heading = this.tooltipHeading();
  }

  // A control that becomes disabled or moves in the DOM on click stops firing mouseleave, so the click hides the tooltip too.
  @HostListener('click')
  @HostListener('mouseleave')
  hide() {
    this.overlayRef?.dispose();
    this.overlayRef = null;
  }

  ngOnDestroy() {
    this.hide();
  }
}
