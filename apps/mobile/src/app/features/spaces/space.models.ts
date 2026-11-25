export type OpportunityType = 'ChairRental' | 'StaffVacancy';
export type WorkspaceType =
  | 'StylingChair'
  | 'BarberChair'
  | 'NailStation'
  | 'MakeupStation'
  | 'TreatmentRoom'
  | 'MassageRoom'
  | 'TattooStation'
  | 'EntireStudio'
  | 'Other';
export type OpportunityStatus = 'Draft' | 'Published' | 'Filled' | 'Paused' | 'Closed';
export type RatePeriod = 'Hour' | 'Day' | 'Week' | 'Month' | 'RevenueShare' | 'Negotiable';

export interface SpaceOpportunity {
  id: string;
  businessId: string;
  businessName: string;
  city: string;
  province: string;
  workspaceType: WorkspaceType;
  opportunityType: OpportunityType;
  status: OpportunityStatus;
  title: string;
  description: string;
  requiredSkills: string;
  includedAmenities: string;
  houseRules: string;
  spacesAvailable: number;
  rateAmount: number | null;
  ratePeriod: RatePeriod;
  startDate: string;
  endDate: string | null;
  recurring: boolean;
  bringOwnClients: boolean;
  approvalRequired: boolean;
  imageUrl: string | null;
  applicationCount: number;
  createdAtUtc: string;
}

export interface SpaceApplication {
  id: string;
  opportunityId: string;
  opportunityTitle: string;
  applicantUserId: string;
  applicantName: string;
  applicantEmail: string;
  message: string;
  portfolioUrl: string | null;
  status: 'Pending' | 'Accepted' | 'Declined' | 'Withdrawn';
  createdAtUtc: string;
}

export const workspaceLabels: Record<WorkspaceType, string> = {
  StylingChair: 'Styling chair',
  BarberChair: 'Barber chair',
  NailStation: 'Nail station',
  MakeupStation: 'Makeup station',
  TreatmentRoom: 'Treatment room',
  MassageRoom: 'Massage room',
  TattooStation: 'Tattoo station',
  EntireStudio: 'Entire studio',
  Other: 'Other workspace',
};
