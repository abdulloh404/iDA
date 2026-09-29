import { useState } from 'react';
import { Navigate, useLocation, useNavigate } from 'react-router-dom';
import { Icon } from '../../components/Icon';
import { ApiErrorAlert } from '../../components/feedback/ApiErrorAlert';
import { ApiError } from '../../api/client';
import { useAuth } from './authState';
export function LoginScreen() {
    const { session, signIn } = useAuth();
    const navigate = useNavigate();
    const location = useLocation();
    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    const [showPassword, setShowPassword] = useState(false);
    const [error, setError] = useState<unknown>(null);
    const [busy, setBusy] = useState(false);
    const fieldError = (name: string) => error instanceof ApiError ? error.fields.find((f) => f.field === name)?.message : undefined;
    if (session) {
        const from = (location.state as {
            from?: string;
        } | null)?.from ?? '/';
        return <Navigate to={from} replace/>;
    }
    async function submit(e: React.FormEvent) {
        e.preventDefault();
        setBusy(true);
        setError(null);
        try {
            await signIn(username, password);
            navigate((location.state as {
                from?: string;
            } | null)?.from ?? '/', { replace: true });
        }
        catch (err) {
            setError(err);
        }
        finally {
            setBusy(false);
        }
    }
    const usernameError = fieldError('username');
    const passwordError = fieldError('password');
    const showBanner = error instanceof ApiError ? error.fields.length === 0 : !!error;
    return (<div className="grid min-h-screen bg-bg lg:grid-cols-[1.05fr_1fr]">
      
      
      <aside className="relative hidden overflow-hidden bg-primary text-inverse lg:block">
        
        <img className="absolute inset-0 size-full object-cover" src="/brand/login-hero.svg" alt="" aria-hidden="true"/>

        <div className="relative flex h-full flex-col justify-between gap-10 p-12 xl:p-16">
          
          <div className="ida-logo ida-logo--reversed ida-logo--lockup h-14 w-78 max-w-full" aria-hidden="true"/>

          <div className="max-w-[46ch]">
            <p className="text-body-lg">
              ระบบบริหารจัดการค่าตอบแทนแพทย์แบบครบวงจร สำหรับเครือโรงพยาบาลพญาไท–เปาโล
              คำนวณ กระทบยอด และออกเอกสารภาษีได้อย่างแม่นยำ
            </p>

            <ul className="mt-8 flex flex-col gap-4">
              {[
            'ข้อมูลแพทย์ส่วนกลาง ใช้ร่วมกันทุกโรงพยาบาลในเครือ',
            'คำนวณค่าแพทย์ 40(2) และ 40(6) พร้อมร่องรอยการแก้ไขทุกครั้ง',
            'แยกข้อมูลรายโรงพยาบาลด้วยสิทธิ์การใช้งาน',
        ].map((point) => (<li className="flex items-start gap-3 text-body" key={point}>
                  <span className="mt-0.5 grid size-6 shrink-0 place-items-center rounded-full bg-white/20">
                    <Icon name="check" size={14}/>
                  </span>
                  <span>{point}</span>
                </li>))}
            </ul>
          </div>
        </div>
      </aside>

      
      <main className="flex flex-col items-center justify-center gap-6 bg-bg px-4 py-10">
        <form className="relative flex w-full max-w-105 flex-col gap-5 overflow-hidden rounded-2xl border border-line bg-surface p-6 shadow-lg sm:p-10" onSubmit={submit} noValidate>
          
          <span className="ida-brand-bar absolute inset-x-0 top-0" aria-hidden="true"/>
          
          <img className="ida-logo mx-auto h-10 w-auto lg:hidden" src="/brand/ida-logo-horizontal.png" alt="iDA · Intelligent Doctor Application" width={1308} height={225}/>

          <div>
            <h1 className="text-h1 text-ink">เข้าสู่ระบบ</h1>
            <p className="mt-1 text-body text-secondary">
              ใช้บัญชีที่ได้รับจากผู้ดูแลระบบของโรงพยาบาลของคุณ
            </p>
          </div>

          {showBanner && <ApiErrorAlert error={error}/>}

          <div className="ida-field">
            <label className="ida-label" htmlFor="login-username">
              ชื่อผู้ใช้งาน
            </label>
            <input id="login-username" className="ida-input" value={username} onChange={(e) => setUsername(e.target.value)} autoComplete="username" autoFocus aria-invalid={!!usernameError} aria-describedby={usernameError ? 'login-username-error' : undefined}/>
            {usernameError && (<p className="ida-field-error" id="login-username-error">
                <Icon name="alert" size={14}/>
                {usernameError}
              </p>)}
          </div>

          <div className="ida-field">
            <label className="ida-label" htmlFor="login-password">
              รหัสผ่าน
            </label>
            
            <div className="relative">
              <input id="login-password" className="ida-input pr-12" type={showPassword ? 'text' : 'password'} value={password} onChange={(e) => setPassword(e.target.value)} autoComplete="current-password" aria-invalid={!!passwordError} aria-describedby={passwordError ? 'login-password-error' : undefined}/>
              <button type="button" className="absolute inset-y-0 right-0 grid w-12 place-items-center rounded-md text-muted hover:text-primary" onClick={() => setShowPassword((shown) => !shown)} aria-label={showPassword ? 'ซ่อนรหัสผ่าน' : 'แสดงรหัสผ่าน'} aria-pressed={showPassword}>
                <Icon name={showPassword ? 'eyeOff' : 'eye'} size={18}/>
              </button>
            </div>
            {passwordError && (<p className="ida-field-error" id="login-password-error">
                <Icon name="alert" size={14}/>
                {passwordError}
              </p>)}
          </div>

          <button type="submit" className="ida-btn ida-btn--primary ida-btn--lg w-full" disabled={busy}>
            {busy && <span className="ida-spinner" aria-hidden="true"/>}
            {busy ? 'กำลังเข้าสู่ระบบ…' : 'เข้าสู่ระบบ'}
          </button>

          <p className="text-center text-caption text-secondary">
            มีปัญหาการเข้าสู่ระบบ? ติดต่อผู้ดูแลระบบของโรงพยาบาล
          </p>
        </form>

        <p className="text-caption text-muted">เครือโรงพยาบาลพญาไท–เปาโล · DA-Next</p>
      </main>
    </div>);
}
