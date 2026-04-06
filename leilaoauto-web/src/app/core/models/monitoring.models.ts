export interface CreateMonitoredVehicleRequest {
  make: string;
  model: string;
  yearFrom: number | null;
  yearTo: number | null;
  vehicleType: number | null;
  uf: string | null;
  vehicleCondition: number | null;
}

export interface MonitoredVehicle {
  id: string;
  make: string;
  model: string;
  yearFrom: number | null;
  yearTo: number | null;
  vehicleType: number | null;
  uf: string | null;
  vehicleCondition: number | null;
  createdAtUtc: string;
}
