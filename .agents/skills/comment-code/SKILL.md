---
name: comment-code
description: "Add, revise, translate, or audit code comments for .NET/C#, React, Next.js, iOS, and Android. Prefer // comments, Thai prose, English technical terms, and optional Better Comments tags, with syntax-appropriate exceptions for markup and required documentation. Use when a coding task creates or changes source comments."
---

# Comment Code

ใช้กฎนี้เมื่อเพิ่ม แก้ แปล หรือตรวจ code comment ใน .NET, React, Next.js, iOS และ Android

## กฎการเขียน

- ใช้ `//` (slash 2 ตัว) เป็นรูปแบบหลักสำหรับ comment ใน source code ทั้งระดับไฟล์, type, field, function, method, statement, branch และ logic ภายใน function
- หาก comment มีหลายบรรทัด ให้ขึ้นต้นแต่ละบรรทัดด้วย `//`
- ใช้รูปแบบอื่นเฉพาะเมื่อ syntax ของบริบทนั้นหรือเครื่องมือ documentation จำเป็นต้องใช้ ตามกฎของแต่ละภาษาด้านล่าง
- เขียนประโยคอธิบายเป็นภาษาไทยเป็นหลัก
- คง technical terms เป็นภาษาอังกฤษตามที่ใช้ใน codebase เช่น `audio service`, `backend`, `event`, `queue`, `session`, `inference`, `runtime`, `config`, `worker` และ `thread`
- คงชื่อ type, function, API, library, product, protocol และ identifier ตามต้นฉบับ
- ใช้คำกริยา คำเชื่อม และคำอธิบายทั่วไปเป็นภาษาไทย อย่าคงคำอังกฤษที่ไม่ใช่ technical term จนประโยคอ่านยาก
- อธิบายเจตนา เงื่อนไข หรือเหตุผลที่ไม่ชัดเจนจาก code หลีกเลี่ยง comment ที่เพียงอ่าน code ซ้ำ

## รูปแบบตามภาษาและแพลตฟอร์ม

| ภาษา / แพลตฟอร์ม | รูปแบบ comment |
| --- | --- |
| .NET / C# | ใช้ `//`; ใช้ `///` พร้อม XML documentation เฉพาะเมื่อจำเป็นต้องสร้างหรือรักษา documentation ของ API |
| React / Next.js | ใช้ `//` ใน JavaScript, TypeScript และส่วนที่เป็น code ของ JSX/TSX; ภายใน JSX markup ใช้ `{/* ... */}` เพราะ `//` ในตำแหน่งนี้อาจกลายเป็นข้อความที่ render |
| iOS / Swift / Objective-C | ใช้ `//` รวมถึง code ใน SwiftUI; ใช้ `///` ใน Swift หรือ documentation comment ของ Objective-C เฉพาะเมื่อจำเป็นต้องสร้างหรือรักษา documentation |
| Android / Kotlin / Java | ใช้ `//` รวมถึง code ใน Jetpack Compose; ใช้ `/** ... */` เฉพาะเมื่อจำเป็นต้องสร้างหรือรักษา KDoc/Javadoc |

- ใน HTML/XML markup รวมถึง Android layout ใช้ `<!-- ... -->`; ใน Razor markup ใช้ `@* ... *@` และในบล็อก C# ใช้ `//`
- ใน JavaScript/TypeScript ใช้ `/** ... */` เฉพาะเมื่อจำเป็นต้องสร้างหรือรักษา JSDoc ที่เครื่องมือใช้
- เมื่อแก้ข้อความใน documentation comment เดิม ให้รักษา marker, XML tags และ annotations ที่เครื่องมือใช้ ไม่แปลงเป็น `//` จนสูญเสีย documentation หรือข้อมูล type
- หลีกเลี่ยง `/* ... */` และ `/** ... */` สำหรับ comment ทั่วไป โดยเว้นเฉพาะกรณี markup หรือ documentation ข้างต้น

## Better Comments

- ใช้ Better Comments เมื่อ tag ช่วยสื่อความหมายจริง และไม่ใช้ตกแต่ง comment ทั่วไป
- ใช้ `// TODO:` สำหรับงานที่ต้องกลับมาทำ
- ใช้ `// !` สำหรับคำเตือนหรือความเสี่ยงสำคัญ
- ใช้ `// ?` สำหรับประเด็นที่ยังต้องตัดสินใจหรือตรวจสอบ
- ใช้ `// *` สำหรับข้อมูลสำคัญที่ควรสังเกต
- เขียนข้อความหลัง tag เป็นภาษาไทย และคง technical terms เป็นภาษาอังกฤษ

## ขอบเขต

- แก้เฉพาะ comment ในขอบเขตที่ผู้ใช้ร้องขอ ห้ามเปลี่ยน behavior ของ code โดยไม่จำเป็น
- ห้ามเพิ่มภาษาไทยใน identifier, string, log, error message หรือ UI text เว้นแต่ผู้ใช้สั่งโดยตรง
- เมื่อแก้ comment เดิม ให้เหลือเวอร์ชันเดียว ห้ามวางภาษาไทยและภาษาอังกฤษซ้ำกัน

## ตัวอย่าง

### .NET / C#

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

// ลดงานซ้ำภายใน batch ก่อนส่ง job ให้ worker
public static class JobBatch
{
    // จำกัดการ deduplicate เฉพาะ batch นี้; retry ข้าม batch ยังต้องใช้ idempotency key
    public static string[] GetUniqueIds(IEnumerable<string> jobIds)
    {
        // คงความแตกต่างของตัวพิมพ์ใหญ่เล็ก เพราะ backend มองเป็นคนละ ID
        return jobIds.Distinct(StringComparer.Ordinal).ToArray();
    }
}

// TODO: เพิ่ม metric สำหรับนับ job ที่ซ้ำกันภายใน batch
```

### TypeScript

```typescript
// ลดการส่ง job ซ้ำภายใน request เดียวก่อนเรียก API
export function getUniqueJobIds(jobIds: readonly string[]): string[] {
  // คงความแตกต่างของตัวพิมพ์ใหญ่เล็ก เพราะ backend มองเป็นคนละ ID
  return [...new Set(jobIds)];
}

// ! การ deduplicate ฝั่ง client ไม่แทนการตรวจ idempotency key ฝั่ง server
// TODO: เพิ่ม metric สำหรับนับ job ที่ซ้ำกันภายใน request
```

### React / Next.js (TSX)

```tsx
type JobStatusProps = { isPending: boolean };

// ให้ผู้ใช้เห็นสถานะระหว่างรอ API ตอบกลับ
export function JobStatus({ isPending }: JobStatusProps) {
  return (
    <section>
      {/* แจ้งว่า request ยังอยู่ระหว่างประมวลผล */}
      {isPending && <span>Submitting...</span>}
    </section>
  );
}
```
