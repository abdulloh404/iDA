# BU Background Worker

Worker นี้ใช้ container image เดียวกันและรันแยกหนึ่ง instance ต่อ BU ใน Kubernetes ขอบเขตปัจจุบันคือเลือกฐานข้อมูลของ BU, ตั้งค่า `app.hospital_id`, เชื่อม RabbitMQ แบบ long-lived และตรวจว่า queue มีอยู่ด้วย passive declaration เท่านั้น ยังไม่มี consumer, `get`, `ack`, `publish`, handler หรือการสร้าง exchange/queue/binding

## การตั้งค่า

ลำดับแหล่งค่าคือ command line > environment variables > `appsettings.Local.json` > `appsettings.{Environment}.json` > `appsettings.json` ส่วนชื่อค่า database ใช้ลำดับต่อไปนี้:

1. `ConnectionStrings:Bu`
2. `ConnectionStrings:<BU_ID>` เช่น `ConnectionStrings:BU01`
3. `<BU_ID>_DB_CONNECTION` เช่น `BU01_DB_CONNECTION`

ถ้า connection string ไม่มี password จะเติมจาก `Database:Password`, `<BU_ID>_DB_PASSWORD` หรือ `BU_DB_PASSWORD` ตามรูปแบบ connection ที่เลือก Worker ต้องได้รับ host, database, runtime username และ password ครบ

- BU: `Bu:Id` หรือ `BU_ID` เช่น `BU01` โดย environment `BU_ID` ใช้แทนค่า development เริ่มต้นได้
- Hospital: `Bu:HospitalId` หรือ `<BU_ID>_HOSPITAL_ID`; ถ้าไม่กำหนดจะใช้ BU ID
- Queue name: `Bu:QueueName`; ถ้าไม่กำหนดจะได้ `jobs.<bu-id ตัวเล็ก>` อัตโนมัติ
- RabbitMQ: `Queue:ConnectionString` หรือ `<BU_ID>_QUEUE_CONNECTION`
- Prefetch: `Queue:PrefetchCount` ค่าเริ่มต้น `10`
- รอบตรวจปกติ: `Worker:HealthIntervalSeconds` ค่าเริ่มต้น `30`
- รอบ retry: `Worker:RetryIntervalSeconds` ค่าเริ่มต้น `5`

Kubernetes inject `Bu__Id`, `Bu__QueueName`, `ConnectionStrings__Bu`, `Database__Password` และ `Queue__ConnectionString` ให้แต่ละ runtime ส่วน local Nx ใช้ `BU_ID` ร่วมกับ `<BU_ID>_DB_CONNECTION`, `<BU_ID>_DB_PASSWORD` และ `<BU_ID>_QUEUE_CONNECTION`

สำหรับ local development ที่ DNS ของ cluster ใช้งานไม่ได้ ให้สร้าง `appsettings.Local.json` ในโฟลเดอร์ worker โดยไม่ copy secret file จาก app อื่น:

```json
{
  "Bu": {
    "Id": "BU01",
    "HospitalId": "BU01"
  },
  "ConnectionStrings": {
    "Bu": "Host=<local-db-host>;Port=5432;Database=<bu-database>;Username=<runtime-user>"
  },
  "Database": {
    "Password": "<local-password>"
  },
  "Queue": {
    "ConnectionString": "amqp://<worker-user>:<password>@<local-rabbitmq-host>:5672/<vhost>"
  }
}
```

อย่า commit `appsettings.Local.json` ที่มี secret และอย่าใส่ cluster DNS หรือ network IP แบบตายตัวใน source code
