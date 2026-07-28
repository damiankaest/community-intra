import {
  Blocks,
  Briefcase,
  Building2,
  CircleHelp,
  ClipboardList,
  Factory,
  FolderKanban,
  Pickaxe,
  Route,
  ScrollText,
  ShieldCheck,
  TriangleAlert,
  Trophy,
  Users,
  Zap,
} from 'lucide-react'

const icons = {
  blocks: Blocks,
  briefcase: Briefcase,
  'building-2': Building2,
  'circle-help': CircleHelp,
  'clipboard-list': ClipboardList,
  factory: Factory,
  'folder-kanban': FolderKanban,
  pickaxe: Pickaxe,
  route: Route,
  'scroll-text': ScrollText,
  'shield-check': ShieldCheck,
  'triangle-alert': TriangleAlert,
  trophy: Trophy,
  users: Users,
  zap: Zap,
} as const

export function ThemeIcon({
  name,
  size = 21,
}: {
  name: string
  size?: number
}) {
  const Icon = icons[name as keyof typeof icons] ?? Building2
  return <Icon aria-hidden="true" size={size} />
}
