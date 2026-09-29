import { Component } from 'react';
import type { ErrorInfo, ReactNode } from 'react';
import { Icon } from '../Icon';
interface Props {
    resetKey?: string;
    children: ReactNode;
}
interface State {
    error: Error | null;
    seenKey?: string;
}
export class ErrorBoundary extends Component<Props, State> {
    override state: State = { error: null };
    static getDerivedStateFromError(error: Error): Partial<State> {
        return { error };
    }
    static getDerivedStateFromProps(props: Props, state: State): Partial<State> | null {
        if (props.resetKey === state.seenKey)
            return null;
        return { error: null, seenKey: props.resetKey };
    }
    override componentDidCatch(error: Error, info: ErrorInfo) {
        console.error('หน้าจอเรนเดอร์ไม่สำเร็จ', error, info.componentStack);
    }
    override render() {
        if (!this.state.error)
            return this.props.children;
        return (<section className="ida-card" role="alert">
        <h2 className="ida-card__title">
          <Icon name="alert" size={20}/> หน้านี้แสดงผลไม่สำเร็จ
        </h2>
        <p className="ida-text-secondary">
          เกิดข้อผิดพลาดในการแสดงผลหน้านี้ ข้อมูลที่บันทึกไว้แล้วไม่ได้รับผลกระทบ
          ลองแสดงใหม่อีกครั้ง หรือไปหน้าอื่นแล้วกลับเข้ามา
        </p>

        
        <p className="ida-details-trace">
          <code>{this.state.error.message}</code>
        </p>

        <div className="ida-form-actions--inline">
          <button type="button" className="ida-btn ida-btn--primary" onClick={() => this.setState({ error: null })}>
            <Icon name="refresh" size={18}/>
            ลองแสดงใหม่
          </button>
        </div>
      </section>);
    }
}
