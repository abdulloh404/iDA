import { z } from 'zod';
import { createCrudApi } from '../../api/crud';
import type { RecordStatus } from '../../api/types';
import type { Crumb } from '../../components/layout/PageHeader';
import { REMARK_FIELD, STATUS_COLUMN, STATUS_FIELD } from './descriptor';
import type { ScreenDescriptor } from './descriptor';
export interface MasterListItem {
    id: string;
    code: string;
    nameTh: string;
    nameEn: string | null;
    status: RecordStatus;
}
export interface MasterDetail extends MasterListItem {
    remark: string | null;
    rowVersion: string;
}
export interface MasterInput {
    code: string;
    nameTh: string;
    nameEn: string | null;
    status: RecordStatus;
    remark: string | null;
}
export interface SimpleMasterOptions {
    id: string;
    resource: string;
    path: string;
    titleTh: string;
    breadcrumb: readonly Crumb[];
    codeLabel: string;
    nameLabel: string;
    emptyHint: string;
    maxCodeLength?: number;
}
export function simpleMasterScreen(options: SimpleMasterOptions): ScreenDescriptor<MasterListItem, MasterDetail, MasterInput> {
    const { codeLabel, nameLabel, maxCodeLength = 20 } = options;
    const schema = z.object({
        code: z
            .string()
            .trim()
            .min(1, `โปรดระบุ${codeLabel}`)
            .max(maxCodeLength, `${codeLabel}ต้องไม่เกิน ${maxCodeLength} ตัวอักษร`),
        nameTh: z.string().trim().min(1, `โปรดระบุชื่อ${nameLabel} (ภาษาไทย)`),
        nameEn: z.string().trim().nullable(),
        status: z.enum(['ACTIVE', 'INACTIVE']),
        remark: z.string().trim().nullable(),
    });
    return {
        id: options.id,
        resource: options.resource,
        path: options.path,
        titleTh: options.titleTh,
        breadcrumb: options.breadcrumb,
        api: createCrudApi<MasterListItem, MasterDetail, MasterInput>(options.resource),
        columns: [
            { key: 'code', header: codeLabel, sortable: true, width: '200px' },
            { key: 'nameTh', header: `${nameLabel} (ภาษาไทย)`, sortable: true },
            { key: 'nameEn', header: `${nameLabel} (ภาษาอังกฤษ)`, sortable: true },
            STATUS_COLUMN,
        ],
        filters: [{ kind: 'status', name: 'status', label: 'สถานะ' }],
        searchHint: `ค้นหา${codeLabel}หรือชื่อ${nameLabel}`,
        defaultSort: 'code',
        rowKey: (row) => row.id,
        emptyHint: options.emptyHint,
        sections: [
            {
                title: 'ข้อมูลทั่วไป',
                fields: [
                    {
                        kind: 'text',
                        name: 'code',
                        label: codeLabel,
                        required: true,
                        maxLength: maxCodeLength,
                        immutableOnEdit: true,
                    },
                    { kind: 'text', name: 'nameTh', label: `ชื่อ${nameLabel} (ภาษาไทย)`, required: true },
                    { kind: 'text', name: 'nameEn', label: `ชื่อ${nameLabel} (ภาษาอังกฤษ)` },
                    STATUS_FIELD,
                    REMARK_FIELD,
                ],
            },
        ],
        schema,
        defaultValues: {
            code: '',
            nameTh: '',
            nameEn: null,
            status: 'ACTIVE',
            remark: null,
        },
        toInput: (detail) => ({
            code: detail.code,
            nameTh: detail.nameTh,
            nameEn: detail.nameEn,
            status: detail.status,
            remark: detail.remark,
        }),
    };
}
