import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-stat-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './stat-card.component.html',
  styleUrl: './stat-card.component.scss'
})
export class StatCardComponent {
  @Input({ required: true }) label!: string;
  @Input({ required: true }) value!: string | number;
  @Input() icon = 'bi-activity';
  @Input() iconBgClass = 'bg-primary-subtle';
  @Input() iconColorClass = 'text-primary';
  @Input() subtitle?: string;
  @Input() badgeText?: string;
  @Input() badgeClass = 'bg-success-subtle text-success';
}
