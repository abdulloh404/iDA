import { Link } from 'react-router-dom';
import { useAuth } from '../auth/authState';
import { MASTER_DATA_SCREENS } from '../master-data/registry';
import { MENU } from '../../nav/menu';
import { Icon } from '../../components/Icon';
import { GROUP_ICONS } from '../../nav/icons';
export function HomeScreen() {
    const { session, can } = useAuth();
    const hospital = session?.hospitals.find((h) => h.hospitalId === session.hospitalId);
    const screens = MASTER_DATA_SCREENS.filter((s) => can(`${s.resource}.read`));
    const scope = [hospital?.hospitalName, hospital?.roleNameTh].filter(Boolean).join(' · ');
    const inGuide = MENU.flatMap((group) => group.items.filter((i) => !i.addition));
    const totalScreens = inGuide.length;
    const builtScreens = inGuide.filter((i) => i.screenId || i.path).length;
    return (<>
      
      <section className="ida-hero-card">
        
        <div className="ida-hero-card__watermark ida-logo ida-logo--reversed ida-logo--mark" aria-hidden="true"/>

        <p className="ida-hero-card__label">{scope}</p>
        <h1 className="ida-hero-card__title">สวัสดี {session?.user.displayName ?? ''}</h1>
        {session && session.hospitals.length > 1 && (<p className="ida-hero-card__meta">
            คุณเข้าถึงได้ {session.hospitals.length} โรงพยาบาล สลับได้จากแถบด้านบน
          </p>)}
      </section>

      <div className="ida-tiles">
        <div className="ida-tile">
          <p className="ida-tile__label">หน้าจอที่เปิดใช้งานแล้ว</p>
          <p className="ida-tile__value">
            {builtScreens}
            <span className="ida-text-muted"> / {totalScreens}</span>
          </p>
          <p className="ida-tile__hint">ทั้งหมด {MENU.length} โมดูล</p>
        </div>

        <div className="ida-tile">
          <p className="ida-tile__label">สิทธิ์ที่คุณมี</p>
          <p className="ida-tile__value">{session?.permissions.length ?? 0}</p>
          <p className="ida-tile__hint">ในโรงพยาบาลที่กำลังใช้งาน</p>
        </div>
      </div>

      <section className="ida-card">
        <h2 className="ida-card__title">ข้อมูลหลัก</h2>
        <div className="ida-shortcut-grid">
          {screens.map((screen) => (<Link key={screen.id} to={screen.path} className="ida-link-card">
              
              <span className="ida-link-card__icon">
                <Icon name={GROUP_ICONS[screen.breadcrumb.at(-1)?.label ?? ''] ?? 'database'} size={18}/>
              </span>
              <span className="ida-link-card__label">{screen.titleTh}</span>
              <Icon name="arrowRight" size={18} className="ida-link-card__go"/>
            </Link>))}
        </div>
      </section>
    </>);
}
