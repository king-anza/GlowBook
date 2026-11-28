import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { IonButton, IonContent, IonHeader, IonInput, IonItem, IonTitle, IonToolbar } from '@ionic/angular';

interface HelpArticle {
  id: string;
  topic: string;
  title: string;
  summary: string;
  steps: string[];
}

@Component({
  selector: 'app-help',
  templateUrl: './help.page.html',
  styleUrls: ['./help.page.scss'],
  imports: [FormsModule, RouterLink, IonHeader, IonToolbar, IonTitle, IonContent, IonItem, IonInput, IonButton],
})
export class HelpPage {
  private readonly route = inject(ActivatedRoute);
  readonly query = signal('');
  readonly selectedTopic = signal(this.route.snapshot.queryParamMap.get('topic') ?? 'all');
  readonly articles: HelpArticle[] = [
    {
      id: 'booking',
      topic: 'bookings',
      title: 'Book, change or cancel an appointment',
      summary: 'Understand availability, pending requests, confirmations and cancellations before you act.',
      steps: [
        'Choose a service and professional from a salon profile.',
        'Select an available time and review the full estimate before requesting it.',
        'Track the request under My bookings. Cancellation availability depends on its current status.',
      ],
    },
    {
      id: 'salons',
      topic: 'salons',
      title: 'How salon profiles and approval work',
      summary: 'Know which details come from a business and what Glow approval means.',
      steps: [
        'Approved businesses have completed the platform review workflow.',
        'Review services, staff, policies, address and independent customer reviews.',
        'Use public contact details only for questions the booking flow does not answer.',
      ],
    },
    {
      id: 'spaces',
      topic: 'spaces',
      title: 'Use Glow Spaces safely',
      summary: 'Find rentals and vacancies, understand matches, and control what an owner can see.',
      steps: [
        'Create a reusable profile with skills, availability and portfolio.',
        'Match reasons explain skill, city, opportunity-type and availability signals.',
        'Your email stays private until acceptance. Use Report for misleading or unsafe listings.',
      ],
    },
    {
      id: 'studio',
      topic: 'professional',
      title: 'Set up and operate your studio',
      summary: 'Move from onboarding to daily bookings, calendar capacity and insights.',
      steps: [
        'Complete all onboarding steps before submitting for approval.',
        'Use Calendar to manage staff schedules, workspaces and blocked time.',
        'Use Dashboard for daily actions and Insights for privacy-safe trends.',
      ],
    },
    {
      id: 'account',
      topic: 'account',
      title: 'Account access and personal data',
      summary: 'Recover access, correct your details, export data or request anonymisation.',
      steps: [
        'Use Account help for verification and password reset requests.',
        'Use Account to update your details or export a copy of your data.',
        'Account anonymisation is permanent and requires confirmation.',
      ],
    },
    {
      id: 'connection',
      topic: 'connection',
      title: 'Offline and slow-connection behavior',
      summary: 'Know which actions are safe when the network is unreliable.',
      steps: [
        'The app may retry read-only requests once.',
        'Booking details cached during this session can remain visible offline.',
        'Bookings, cancellations and other changes are never queued or repeated automatically. Reconnect and submit once.',
      ],
    },
  ];
  readonly filtered = computed(() => {
    const term = this.query().trim().toLowerCase();
    return this.articles.filter(
      (article) =>
        (this.selectedTopic() === 'all' || article.topic === this.selectedTopic()) &&
        (!term || `${article.title} ${article.summary} ${article.steps.join(' ')}`.toLowerCase().includes(term)),
    );
  });

  chooseTopic(topic: string): void {
    this.selectedTopic.set(topic);
  }

  search(value: string | null): void {
    this.query.set(value ?? '');
    if (this.query().trim()) this.selectedTopic.set('all');
  }
}
