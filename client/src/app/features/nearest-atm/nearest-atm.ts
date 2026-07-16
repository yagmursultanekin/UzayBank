import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import * as L from 'leaflet';
import { BranchService } from '../../core/services/branch.service';
import { Branch } from '../../core/models/branch.model';
import { TranslateService, TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-nearest-atm',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe],
  templateUrl: './nearest-atm.html',
  styleUrl: './nearest-atm.scss'
})
export class NearestAtmComponent implements OnInit, OnDestroy {
  branches: Branch[] = [];
  filteredBranches: Branch[] = [];
  isLoading = true;
  errorMessage = '';
  typeFilter: 'all' | 'ATM' | 'Şube' = 'all';
  distanceKm = 1;

  private map: L.Map | null = null;
  private markers: L.Marker[] = [];
  private userLat = 41.032575;  // Konum izni verilmezse varsayılan (İstanbul)
  private userLng = 29.110119;

  constructor(
    private branchService: BranchService,
    private router: Router,
    private translate: TranslateService
  ) {}

  ngOnInit(): void {
    this.getUserLocation();
  }

  ngOnDestroy(): void {
    this.map?.remove();
  }

  private getUserLocation(): void {
    if (!navigator.geolocation) {
      this.initMapAndLoad();
      return;
    }

    navigator.geolocation.getCurrentPosition(
      (position) => {
        this.userLat = position.coords.latitude;
        this.userLng = position.coords.longitude;
        this.initMapAndLoad();
      },
      () => {
        // İzin verilmedi — varsayılan konumla devam
        this.initMapAndLoad();
      }
    );
  }

  private initMapAndLoad(): void {
    this.map = L.map('branch-map').setView([this.userLat, this.userLng], 15);

    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '&copy; OpenStreetMap contributors'
    }).addTo(this.map);

    // Kullanıcının konumu (mavi daire)
    L.circleMarker([this.userLat, this.userLng], {
      radius: 8,
      color: '#0f3460',
      fillColor: '#0f3460',
      fillOpacity: 0.9
    }).addTo(this.map).bindPopup(this.translate.instant('ATM.YOUR_LOCATION'));

    this.loadBranches();
  }

  loadBranches(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.branchService.getNearest(this.userLat, this.userLng, this.distanceKm).subscribe({
      next: (data) => {
        this.branches = data;
        this.applyFilter();
        this.isLoading = false;
      },
      error: () => {
        this.errorMessage = 'ATM.LOAD_ERROR';
        this.isLoading = false;
      }
    });
  }

  applyFilter(): void {
    this.filteredBranches = this.typeFilter === 'all'
      ? this.branches
      : this.branches.filter(b => b.type === this.typeFilter);
    this.updateMarkers();
  }

  private updateMarkers(): void {
    if (!this.map) return;

    this.markers.forEach(m => m.remove());
    this.markers = [];

    for (const branch of this.filteredBranches) {
      const icon = L.divIcon({
        className: 'branch-marker',
        html: branch.type === 'ATM' ? '🏧' : '🏦',
        iconSize: [30, 30],
        iconAnchor: [15, 15]
      });

      const km = this.translate.instant('ATM.KM');
      const marker = L.marker([branch.latitude, branch.longitude], { icon })
        .addTo(this.map)
        .bindPopup(`<b>${branch.name}</b><br>${branch.address}<br>${branch.distanceKm.toFixed(2)} ${km}`);

      this.markers.push(marker);
    }
  }

  focusBranch(branch: Branch): void {
    this.map?.setView([branch.latitude, branch.longitude], 17);
    const marker = this.markers.find(m =>
      m.getLatLng().lat === branch.latitude && m.getLatLng().lng === branch.longitude);
    marker?.openPopup();
  }

  goBack(): void {
    this.router.navigate(['/dashboard']);
  }
}