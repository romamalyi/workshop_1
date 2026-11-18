import { afterNextRender, DestroyRef, Directive, ElementRef, HostListener, inject, output } from '@angular/core';

const openDialogs: DialogDirective[] = [];

@Directive({
  selector: '[appDialog]',
  standalone: true,
  host: { tabindex: '-1', style: 'outline: none' },
})
export class DialogDirective {
  readonly dialogClose = output<void>();
  private readonly element = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;

  constructor() {
    openDialogs.push(this);
    afterNextRender(() => this.element.focus({ preventScroll: true }));
    inject(DestroyRef).onDestroy(() => openDialogs.splice(openDialogs.indexOf(this), 1));
  }

  @HostListener('document:keydown.escape', ['$event'])
  onEscape(event: Event): void {
    if (event.defaultPrevented || (event as KeyboardEvent).isComposing || openDialogs.at(-1) !== this) return;
    event.preventDefault();
    this.dialogClose.emit();
  }

  @HostListener('click', ['$event'])
  onBackdropClick(event: Event): void {
    if (event.target === this.element) this.dialogClose.emit();
  }
}
