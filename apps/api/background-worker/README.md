# BU Background Worker

Worker นี้ใช้ source/image ร่วมกันและแต่ละ instance เลือก BU ผ่าน runtime configuration ขอบเขตปัจจุบันคือเลือกฐานข้อมูลของ BU, ตั้งค่า `app.hospital_id`, เชื่อม RabbitMQ แบบ long-lived และตรวจว่า queue มีอยู่ด้วย passive declaration เท่านั้น ยังไม่มี consumer, `get`, `ack`, `publish`, handler หรือการสร้าง exchange/queue/binding

## การตั้งค่า

ลำดับแหล่งค่าคือ command line > environment variables > `appsettings.Local.json` > `appsettings.{Environment}.json` > `appsettings.json` ส่วนชื่อค่า database ใช้ลำดับต่อไปนี้:

1. `ConnectionStrings:Bu`
2. `<BU_ID>_DB_CONNECTION` เช่น `BU01_DB_CONNECTION`
3. `ConnectionStrings:<BU_ID>` เช่น `ConnectionStrings:BU01`
4. `ConnectionStrings:<BU_ID>` จาก `apps/api/core-api/api/appsettings.Local.json` เฉพาะ Development

Worker อ่านจากไฟล์ API เฉพาะ runtime connection ของ BU ที่เลือก ไม่ได้นำค่า Core, JWT, admin หรือ migration เข้ามา ถ้า connection string ไม่มี password จะเติมจาก `Database:Password`, `<BU_ID>_DB_PASSWORD` หรือ `BU_DB_PASSWORD` ตามรูปแบบ connection ที่เลือก Worker ต้องได้รับ host, database, runtime username และ password ครบ

- BU: `Bu:Id` หรือ `BU_ID` เช่น `BU01` โดย Development เริ่มต้นเป็น BU01 และ environment `BU_ID` ใช้เปลี่ยนสาขาได้
- Hospital: `Bu:HospitalId` หรือ `<BU_ID>_HOSPITAL_ID`; ถ้าไม่กำหนดจะใช้ BU ID
- Queue name: `Bu:QueueName`; ถ้าไม่กำหนดจะได้ `jobs.<bu-id ตัวเล็ก>` อัตโนมัติ
- RabbitMQ: `Queue:ConnectionString` หรือ `<BU_ID>_QUEUE_CONNECTION`
- Prefetch: `Queue:PrefetchCount` ค่าเริ่มต้น `10`
- รอบตรวจปกติ: `Worker:HealthIntervalSeconds` ค่าเริ่มต้น `30`
- รอบ retry: `Worker:RetryIntervalSeconds` ค่าเริ่มต้น `5`

PostgreSQL ใช้ฐานภายนอกตาม connection ของ BU ที่เลือก ส่วน RabbitMQ ใช้ service ที่มีอยู่ที่ `localhost:5672` และ vhost `ida` ต้องเตรียมบัญชี สิทธิ์ และ queue `jobs.<bu-id ตัวเล็ก>` ให้พร้อมก่อนเริ่ม Worker

สำหรับ local development ให้ตั้ง `BU_ID` เพื่อเลือก `<BU_ID>_DB_CONNECTION`, `<BU_ID>_DB_PASSWORD` และ `<BU_ID>_QUEUE_CONNECTION` ใน `.env` Worker ใช้ Queue URI ที่กำหนดโดยตรง ตัวอย่าง override เฉพาะ worker ใน `appsettings.Local.json`:

```json
{
  "Bu": {
    "Id": "BU01",
    "HospitalId": "BU01"
  },
  "Queue": {
    "ConnectionString": "amqp://bu01_worker:REPLACE_ME_QUEUE_PASSWORD@localhost:5672/ida"
  }
}
```

`Queue:ConnectionString` มีลำดับก่อน `<BU_ID>_QUEUE_CONNECTION`; ทั้งสองแหล่งใช้ host, port, credentials และ vhost ตาม URI ที่ระบุ

อย่า commit `appsettings.Local.json` ที่มี secret และอย่าใส่ network IP แบบตายตัวใน source code
