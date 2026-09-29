# BU Background Worker

Worker นี้ใช้ container image เดียวกันและรันแยกหนึ่ง instance ต่อ BU ใน Kubernetes ขอบเขตปัจจุบันคือเลือกฐานข้อมูลของ BU, ตั้งค่า `app.hospital_id`, เชื่อม RabbitMQ แบบ long-lived และตรวจว่า queue มีอยู่ด้วย passive declaration เท่านั้น ยังไม่มี consumer, `get`, `ack`, `publish`, handler หรือการสร้าง exchange/queue/binding

## การตั้งค่า

ลำดับแหล่งค่าคือ command line > environment variables > `appsettings.Local.json` > `appsettings.{Environment}.json` > `appsettings.json` ส่วนชื่อค่า database ใช้ลำดับต่อไปนี้:

1. `ConnectionStrings:Bu`
2. `ConnectionStrings:<BU_ID>` เช่น `ConnectionStrings:BU01`
3. `ConnectionStrings:<BU_ID>` จาก `apps/api/core-api/api/appsettings.Local.json` เฉพาะ Development
4. `<BU_ID>_DB_CONNECTION` เช่น `BU01_DB_CONNECTION`

Worker อ่านจากไฟล์ API เฉพาะ runtime connection ของ BU ที่เลือก ไม่ได้นำค่า Core, JWT, admin หรือ migration เข้ามา ถ้า connection string ไม่มี password จะเติมจาก `Database:Password`, `<BU_ID>_DB_PASSWORD` หรือ `BU_DB_PASSWORD` ตามรูปแบบ connection ที่เลือก Worker ต้องได้รับ host, database, runtime username และ password ครบ

- BU: `Bu:Id` หรือ `BU_ID` เช่น `BU01` โดย environment `BU_ID` ใช้แทนค่า development เริ่มต้นได้
- Hospital: `Bu:HospitalId` หรือ `<BU_ID>_HOSPITAL_ID`; ถ้าไม่กำหนดจะใช้ BU ID
- Queue name: `Bu:QueueName`; ถ้าไม่กำหนดจะได้ `jobs.<bu-id ตัวเล็ก>` อัตโนมัติ
- RabbitMQ: `Queue:ConnectionString` หรือ `<BU_ID>_QUEUE_CONNECTION`
- RabbitMQ local endpoint: `Queue:Host` ค่า Development เริ่มต้น `localhost`; พอร์ตใช้ `Queue:Port`, `KUBE_QUEUE_PORT_FORWARD_PORT`, แล้วจึง `5672`
- Prefetch: `Queue:PrefetchCount` ค่าเริ่มต้น `10`
- รอบตรวจปกติ: `Worker:HealthIntervalSeconds` ค่าเริ่มต้น `30`
- รอบ retry: `Worker:RetryIntervalSeconds` ค่าเริ่มต้น `5`

Kubernetes inject `Bu__Id`, `Bu__QueueName`, `ConnectionStrings__Bu`, `Database__Password` และ `Queue__ConnectionString` ให้แต่ละ runtime ค่า `Queue__ConnectionString` ที่ระบุโดยตรงจะใช้ endpoint เดิมทุกประการและไม่ถูกเขียนทับด้วยค่า local

สำหรับ local development ให้ตั้ง `BU_ID` เพื่อเลือก `ConnectionStrings:<BU_ID>` จาก `apps/api/core-api/api/appsettings.Local.json` และใช้ `<BU_ID>_QUEUE_CONNECTION` เดิมสำหรับ username, password และ vhost Worker จะเปลี่ยนเฉพาะ host/port ไปยัง port-forward ที่ `localhost:5672` โดยอัตโนมัติ ตัวอย่าง override เฉพาะ worker:

```json
{
  "Bu": {
    "Id": "BU01",
    "HospitalId": "BU01"
  },
  "Queue": {
    "Host": "localhost",
    "Port": 5672
  }
}
```

ถ้ากำหนด `Queue:ConnectionString` ใน worker โดยตรง ระบบจะไม่แก้ host/port ของ URI นั้น ส่วน fallback `<BU_ID>_QUEUE_CONNECTION` จะคง credentials และ vhost เดิมไว้ เปลี่ยนเพียง endpoint ตามค่าข้างต้น

อย่า commit `appsettings.Local.json` ที่มี secret และอย่าใส่ cluster DNS หรือ network IP แบบตายตัวใน source code
