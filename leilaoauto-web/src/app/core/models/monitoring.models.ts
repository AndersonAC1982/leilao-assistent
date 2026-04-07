export interface CreateMonitoredVehicleRequest {
  brand: string;
  model: string;
  year: number;
  type: number;
  uf: string;
  vehicleState: number;
}

export interface UpdateMonitoredVehicleRequest {
  brand: string;
  model: string;
  year: number;
  type: number;
  uf: string;
  vehicleState: number;
}

export interface MonitoredVehicle {
  id: string;
  brand: string;
  model: string;
  year: number;
  type: number;
  uf: string;
  vehicleState: number;
  createdAt: string;
}
