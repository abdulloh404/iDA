import { useState } from 'react';
import { ApiError } from '../../api/client';
import { Icon } from '../Icon';
interface ErrorCopy {
    connectionFailed: string;
    details: string;
    referenceId: string;
}
export function ApiErrorAlert({ error, copy }: {
    error: unknown;
    copy?: ErrorCopy;
}) {
    const [showDetails, setShowDetails] = useState(false);
    if (!error)
        return null;
    const isApiError = error instanceof ApiError;
    const message = isApiError
        ? error.message
        : copy?.connectionFailed ?? 'เชื่อมต่อระบบไม่ได้ กรุณาลองใหม่อีกครั้ง';
    const traceId = isApiError ? error.traceId : undefined;
    return (<div className="ida-alert ida-alert--error" role="alert">
      <div className="ida-alert__body">
        <Icon name="alert" size={18}/>
        <span>{message}</span>
      </div>

      {traceId && (<>
          <button type="button" className="ida-details-toggle" onClick={() => setShowDetails((open) => !open)} aria-expanded={showDetails}>
            {copy?.details ?? 'รายละเอียด'}
          </button>
          {showDetails && (<p className="ida-details-trace">
              {copy?.referenceId ?? 'รหัสอ้างอิง'}: <code>{traceId}</code>
            </p>)}
        </>)}
    </div>);
}
