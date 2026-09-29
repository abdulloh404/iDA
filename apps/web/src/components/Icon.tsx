import { ArrowDown, ArrowRight, ArrowUp, Ban, Banknote, Bold, Building2, Calculator, CalendarDays, ChartColumn, Check, ChevronDown, ChevronRight, ChevronsUpDown, ClipboardList, Clock, Database, Download, Eye, EyeOff, FileText, Heading2, Hospital, Inbox, Info, Italic, KeyRound, Layers, Link2, List, ListOrdered, ListFilter, LogOut, Mail, MapPin, Menu, Monitor, Moon, Percent, Plus, Quote, Redo2, Receipt, Search, Settings, ShieldCheck, SquarePen, Sun, Trash2, Underline, TriangleAlert, RotateCcw, Undo2, Users, X, } from 'lucide-react';
import type { LucideIcon } from 'lucide-react';
const ICONS = {
    menu: Menu,
    chevronDown: ChevronDown,
    chevronRight: ChevronRight,
    search: Search,
    hospital: Hospital,
    building: Building2,
    logout: LogOut,
    plus: Plus,
    download: Download,
    pencil: SquarePen,
    eye: Eye,
    eyeOff: EyeOff,
    trash: Trash2,
    check: Check,
    close: X,
    alert: TriangleAlert,
    info: Info,
    arrowRight: ArrowRight,
    arrowUp: ArrowUp,
    arrowDown: ArrowDown,
    sortable: ChevronsUpDown,
    ban: Ban,
    undo: Undo2,
    refresh: RotateCcw,
    inbox: Inbox,
    home: Building2,
    chart: ChartColumn,
    clipboard: ClipboardList,
    database: Database,
    banknote: Banknote,
    percent: Percent,
    users: Users,
    layers: Layers,
    filter: ListFilter,
    shield: ShieldCheck,
    clock: Clock,
    calendar: CalendarDays,
    calculator: Calculator,
    fileText: FileText,
    receipt: Receipt,
    settings: Settings,
    monitor: Monitor,
    sun: Sun,
    moon: Moon,
    bold: Bold,
    italic: Italic,
    underline: Underline,
    heading: Heading2,
    listBullet: List,
    listOrdered: ListOrdered,
    quote: Quote,
    link: Link2,
    redo: Redo2,
    key: KeyRound,
    mail: Mail,
    mapPin: MapPin,
} satisfies Record<string, LucideIcon>;
export type IconName = keyof typeof ICONS;
export interface IconProps {
    name: IconName;
    size?: number;
    title?: string;
    className?: string;
}
export function Icon({ name, size = 20, title, className }: IconProps) {
    const Glyph = ICONS[name];
    return (<Glyph className={className} size={size} strokeWidth={1.75} style={{ flexShrink: 0 }} role={title ? 'img' : undefined} aria-label={title} aria-hidden={title ? undefined : true} focusable="false"/>);
}
