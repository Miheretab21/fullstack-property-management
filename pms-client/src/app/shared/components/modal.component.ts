import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './modal.component.html',
  styleUrl: './modal.component.scss'
})
export class ModalComponent {
  @Input() isOpen = false;
  @Input({ required: true }) title!: string;
  @Input() icon?: string;
  @Input() showFooter = true;
  @Input() modalSizeClass = '';
  @Input() closeOnBackdrop = true;

  @Output() closed = new EventEmitter<void>();
  @Output() closeModal = new EventEmitter<void>();

  close() {
    this.isOpen = false;
    this.closed.emit();
    this.closeModal.emit();
  }

  onBackdropClick(event: MouseEvent) {
    if (this.closeOnBackdrop) {
      this.close();
    }
  }
}
