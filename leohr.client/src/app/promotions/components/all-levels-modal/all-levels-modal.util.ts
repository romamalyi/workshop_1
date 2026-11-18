import { LevelNode, LevelNodeStatus } from '../../models/all-levels-modal.model';
import { Technology } from '../../../promotion-criteria/promotion-criteria.model';

interface Point {
  x: number;
  y: number;
}

function formatStepPath(prev: Point, curr: Point, radius = 12): string {
  const midX = (prev.x + curr.x) / 2;
  const dy = curr.y - prev.y;
  const dirY = dy < 0 ? -1 : 1;
  const r = Math.min(radius, Math.abs(midX - prev.x), Math.abs(dy) / 2);

  if (r <= 0 || Math.abs(dy) === 0) {
    return ` L ${midX},${prev.y} L ${midX},${curr.y} L ${curr.x},${curr.y}`;
  }

  return ` L ${midX - r},${prev.y} Q ${midX},${prev.y} ${midX},${prev.y + dirY * r} L ${midX},${curr.y - dirY * r} Q ${midX},${curr.y} ${midX + r},${curr.y} L ${curr.x},${curr.y}`;
}

export function buildCompletedSvgPath(nodes: LevelNode[], currIdx: number): string {
  if (!nodes.length || currIdx < 0) return '';
  const targetEnd = Math.min(currIdx, nodes.length - 1);
  let path = `M 0,${nodes[0].y} L ${nodes[0].x},${nodes[0].y}`;
  for (let i = 1; i <= targetEnd; i++) {
    path += formatStepPath(nodes[i - 1], nodes[i]);
  }
  return path;
}

export function buildHalfWaySvgPath(nodes: LevelNode[], currIdx: number): string {
  if (!nodes.length || currIdx < 0 || currIdx >= nodes.length - 1) return '';
  const p0 = nodes[currIdx];
  const p3 = nodes[currIdx + 1];
  const midX = (p0.x + p3.x) / 2;
  const midY = (p0.y + p3.y) / 2;
  const dy = p3.y - p0.y;
  const dirY = dy < 0 ? -1 : 1;
  const r = Math.min(12, Math.abs(midX - p0.x), Math.abs(dy) / 2);

  if (r <= 0 || Math.abs(dy) === 0) {
    return `M ${p0.x},${p0.y} L ${midX},${p0.y} L ${midX},${midY}`;
  }

  return `M ${p0.x},${p0.y} L ${midX - r},${p0.y} Q ${midX},${p0.y} ${midX},${p0.y + dirY * r} L ${midX},${midY}`;
}

export function buildFutureSvgPath(nodes: LevelNode[], currIdx: number, stepWidth: number): string {
  if (!nodes.length) return '';
  if (currIdx < 0) {
    let path = `M 0,${nodes[0].y} L ${nodes[0].x},${nodes[0].y}`;
    for (let i = 1; i < nodes.length; i++) {
      path += formatStepPath(nodes[i - 1], nodes[i]);
    }
    const last = nodes[nodes.length - 1];
    path += ` L ${last.x + stepWidth / 2},${last.y}`;
    return path;
  }

  let path = '';
  if (currIdx < nodes.length - 1) {
    const p0 = nodes[currIdx];
    const p3 = nodes[currIdx + 1];
    const midX = (p0.x + p3.x) / 2;
    const midY = (p0.y + p3.y) / 2;
    const dy = p3.y - p0.y;
    const dirY = dy < 0 ? -1 : 1;
    const r = Math.min(12, Math.abs(midX - p0.x), Math.abs(dy) / 2);

    if (r <= 0 || Math.abs(dy) === 0) {
      path = `M ${midX},${midY} L ${midX},${p3.y} L ${p3.x},${p3.y}`;
    } else {
      path = `M ${midX},${midY} L ${midX},${p3.y - dirY * r} Q ${midX},${p3.y} ${midX + r},${p3.y} L ${p3.x},${p3.y}`;
    }

    for (let i = currIdx + 2; i < nodes.length; i++) {
      path += formatStepPath(nodes[i - 1], nodes[i]);
    }
    const last = nodes[nodes.length - 1];
    path += ` L ${last.x + stepWidth / 2},${last.y}`;
  }
  return path;
}

export function resolveLevelNodeStatus(idx: number, fromIdx: number, toIdx: number): LevelNodeStatus {
  if (idx < fromIdx) return LevelNodeStatus.Passed;
  if (idx === fromIdx) return LevelNodeStatus.Current;
  if (idx === toIdx) return LevelNodeStatus.Next;
  return LevelNodeStatus.Locked;
}

export function calculatePromotionIndexes(
  roles: string[],
  currentRoleTarget?: string,
  targetRole?: string,
): { fromIdx: number; toIdx: number } {
  let fromIdx = roles.findIndex((role) => role.toLowerCase() === currentRoleTarget?.toLowerCase());
  let toIdx = roles.findIndex((role) => role.toLowerCase() === targetRole?.toLowerCase());
  if (fromIdx === -1) fromIdx = 0;
  if (toIdx === -1 || toIdx <= fromIdx) toIdx = fromIdx + 1;
  return { fromIdx, toIdx };
}

export function normalizeTechnology(rawTech: string | null | undefined): Technology | null {
  if (!rawTech) return null;
  const clean = rawTech
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]/g, '');
  if (clean.includes('net')) return Technology.Dotnet;
  if (clean.includes('web') || clean.includes('front')) return Technology.WebUi;
  if (clean.includes('ios')) return Technology.Ios;
  if (clean.includes('android')) return Technology.Android;
  if (clean.includes('php')) return Technology.Php;
  if (clean.includes('ruby') || clean.includes('ror')) return Technology.Ruby;
  if (clean.includes('flutter')) return Technology.Flutter;
  return null;
}
