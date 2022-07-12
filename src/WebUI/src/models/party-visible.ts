import { Point } from 'geojson';
import Platform from './platform';
import Region from './region';

export default interface PartyVisible {
  id: number;
  platform: Platform;
  platformUserId: string;
  name: string;
  region: Region;
  troops: number;
  position: Point;
}