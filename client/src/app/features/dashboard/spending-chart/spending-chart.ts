import { Component, Input, OnChanges, ViewChild, ElementRef, AfterViewInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateService,TranslatePipe} from '@ngx-translate/core';
import { Chart, registerables } from 'chart.js';
import { Transaction, TransactionType } from '../../../core/models/transaction.model';
Chart.register(...registerables);

@Component({
  selector: 'app-spending-chart',
  standalone: true,
  imports: [CommonModule, TranslatePipe],
  templateUrl: './spending-chart.html',
  styleUrl: './spending-chart.scss'
})
export class SpendingChartComponent implements AfterViewInit, OnChanges {
  @Input() transactions: Transaction[] = [];

  @ViewChild('lineCanvas') lineCanvas!: ElementRef<HTMLCanvasElement>;
  @ViewChild('doughnutCanvas') doughnutCanvas!: ElementRef<HTMLCanvasElement>;

  private lineChart?: Chart;
  private doughnutChart?: Chart;

  // Chart.js metinleri HTML'de değil TS'te üretiliyor — pipe kullanamıyoruz,
  // bu yüzden TranslateService'i doğrudan çağırıyoruz.
  private translate = inject(TranslateService);

  ngAfterViewInit(): void {
    this.buildCharts();

    // Dil değişince grafikleri yeniden çiz (etiketler güncellensin)
    this.translate.onLangChange.subscribe(() => {
      if (this.lineCanvas && this.doughnutCanvas) {
        this.buildCharts();
      }
    });
  }

  ngOnChanges(): void {
    if (this.lineCanvas && this.doughnutCanvas) {
      this.buildCharts();
    }
  }

  private buildCharts(): void {
    this.buildLineChart();
    this.buildDoughnutChart();
  }

  private buildLineChart(): void {
    this.lineChart?.destroy();

    // İşlemleri tarihe göre sırala (eski → yeni)
    const sorted = [...this.transactions].sort(
      (a, b) => new Date(a.transactionDate).getTime() - new Date(b.transactionDate).getTime()
    );

    // Aktif dile göre tarih formatı (tr-TR / en-US)
    const locale = this.translate.getCurrentLang() === 'en' ? 'en-US' : 'tr-TR';

    const labels = sorted.map(t =>
      new Date(t.transactionDate).toLocaleDateString(locale, { day: '2-digit', month: '2-digit' })
    );
    const balances = sorted.map(t => t.balanceAfterTransaction);

    this.lineChart = new Chart(this.lineCanvas.nativeElement, {
      type: 'line',
      data: {
        labels: labels,
        datasets: [{
          label: this.translate.instant('ANALYTICS.BALANCE'),
          data: balances,
          borderColor: '#0f3460',
          backgroundColor: 'rgba(15, 52, 96, 0.08)',
          fill: true,
          tension: 0.35,
          pointBackgroundColor: '#0f3460'
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { display: false }
        }
      }
    });
  }

  private buildDoughnutChart(): void {
    this.doughnutChart?.destroy();

    const totalCredit = this.transactions
      .filter(t => t.type === TransactionType.Credit)
      .reduce((sum, t) => sum + t.amount, 0);

    const totalDebit = this.transactions
      .filter(t => t.type === TransactionType.Debit)
      .reduce((sum, t) => sum + t.amount, 0);

    this.doughnutChart = new Chart(this.doughnutCanvas.nativeElement, {
      type: 'doughnut',
      data: {
        labels: [
          this.translate.instant('ANALYTICS.INCOMING'),
          this.translate.instant('ANALYTICS.OUTGOING')
        ],
        datasets: [{
          data: [totalCredit, totalDebit],
          backgroundColor: ['#2ecc71', '#e53e3e'],
          borderWidth: 0
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        cutout: '65%',
        plugins: {
          legend: { position: 'bottom' }
        }
      }
    });
  }
}