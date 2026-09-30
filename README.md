# iDA — Nx monorepo

โครงเริ่มต้นสำหรับ Flutter (Android/iOS), React + Vite และ backend .NET 9 ภายใต้ Nx

ชื่อแสดงผลของระบบคือ `iDA`; package scope, Docker images และ RabbitMQ vhost ใช้ `ida`
Core และทุก BU ใช้ PostgreSQL ภายนอกตาม `.env`; ตัวอย่างปัจจุบันใช้ database `core`, `bu01`, `bu02`
ผู้ให้บริการต้องสร้าง database ไว้แล้ว ส่วน `db:migrate` เตรียม runtime roles, schema และตารางผ่านการเชื่อมต่อ PostgreSQL โดยไม่ต้อง SSH
RabbitMQ ใช้ service ที่มีอยู่ที่ `localhost:5672` และ vhost `ida`; ต้องเตรียมบัญชี สิทธิ์ และ queues ให้พร้อมก่อนเริ่มแอป
Android/iOS ใช้ app identifier ใหม่ `com.ida.mobile`; iOS ต้องเตรียม signing/provisioning ให้ตรงกับ identifier นี้
ลิงก์ repository ใน package metadata ใช้ `/ida` แล้ว แต่ไม่ได้ rename repository บน server, ย้าย checkout folder หรือเปลี่ยน Git remote จริง

## โครงสร้าง

```text
project/
├── apps/
│   ├── mobile/                 Flutter app เดียวสำหรับ Android และ iOS
│   │   ├── lib/
│   │   ├── android/
│   │   └── ios/Runner/         รวม native Swift
│   ├── web/                    React + Vite frontend กลาง (SPA)
│   └── api/                    backend solution กลาง
│       ├── iDA.sln
│       ├── core-api/           Core API กลางหนึ่ง service
│       └── background-worker/  งานประมวลผลเบื้องหลัง ใช้ source/image ร่วมกันทุก BU
├── .agents/skills/             เฉพาะ Nx skills สำหรับ Codex
├── .claude/skills/             symlink ไป Nx skills ชุดเดียวกัน
├── .codex/config.toml          Nx MCP สำหรับ Codex
├── .mcp.json                   Nx MCP สำหรับ Claude
├── compose.yaml
├── nx.json
└── package.json
```

## Backend และ BU

Backend อยู่ใน solution เดียว มี Core API, BU Worker และ Ingest Worker
แต่ละ BU ใช้ source เดียวกันและเลือก database ผ่าน configuration

- Core API กลางเชื่อม **Core Database 1 ก้อน** และฐาน BU ตามสาขาใน JWT โดยอ่านข้อมูลแต่ละฐานแยกกันแล้วประกอบผลใน API
- ทุก BU ใช้ Worker image `ida/bu-worker` เดียวกัน เปลี่ยนเฉพาะ runtime configuration
- Worker แต่ละ instance ใช้ runtime configuration ของ BU ที่เลือก; PostgreSQL อยู่บน server ภายนอก
- `Bu__Id=BU01` คู่กับ `Bu__QueueName=jobs.bu01` และ `ConnectionStrings__Bu` ของ BU01
- BU Database แยกกันตาม Host/Port/Database พร้อมบัญชี runtime ของแต่ละสาขา
- Worker เข้าข้อมูลกลางผ่าน Core API; ไม่ได้รับ credential ของ Core DB
- RabbitMQ ที่ `localhost:5672` แยก queue และ ACL/credential ต่อ BU ใน vhost `ida`
- Web/Mobile ใช้งานร่วมกันทุก BU; Mobile เป็น client บนอุปกรณ์

`background-worker` คือโปรเจกต์งานประมวลผลเบื้องหลังที่ใช้ร่วมกันทุกสาขา ไม่ใช่ source แยกต่อสาขา
เพิ่ม replicas ได้ภายหลังเมื่อทำการ claim งาน, idempotency และ concurrency control แล้ว

ฐาน Core เก็บข้อมูลและ enum ส่วนกลางใน schema `core` ส่วนฐาน BU เก็บตารางและ enum ของตนเองใน schema `bu` โดยใช้ enum definition ชุดเดียวกันในโค้ด ฐาน BU ไม่มี schema `core` หรือ foreign table ที่เชื่อมกลับไปยัง Core

หลังอัปเดตโค้ดให้รัน `npm run db:migrate` เพื่อปรับ Core และทุก BU ที่ตั้งค่าไว้ สำหรับฐาน BU เดิม migration จะย้าย enum จาก `core` ไป `bu`, ถอด foreign tables ของ Core ที่ระบบสร้างไว้ และลบ schema `core` เมื่อว่าง โดยไม่ลบข้อมูลตาราง BU หากพบชนิดข้อมูลซ้ำหรือวัตถุอื่นขวางการลบ schema จะหยุดและ rollback ฐานนั้นแทนการลบแบบ cascade จากนั้นเริ่ม API/worker ใหม่เพื่อโหลดการตั้งค่า enum ใหม่

## สถานะ scaffold

| ส่วน | มีแล้ว | ยังต้องทำต่อ |
| --- | --- | --- |
| Web | React + Vite SPA + static-server Dockerfile | หน้าใช้งานและ API integration |
| Mobile | Flutter starter, Android และ iOS/Swift | หน้าจอและ API integration |
| Core API | .NET 9, `GET /health`, OpenAPI ใน Development | DB client, authentication, queue publisher/orchestrator, business endpoints |
| Worker | เลือก BU DB, ตั้ง hospital context, ตรวจ DB/Queue, รองรับ shutdown | queue consumer, ตรวจ tenant ของ message, retry/DLQ, business jobs |
| Docker | Compose สำหรับ Core API, Web และ 3 Workers | ตั้งปลายทาง/credential และ build images |

BU Worker ตรวจการเชื่อมต่อ DB และตรวจว่ามี queue อยู่แล้ว ยังไม่มี consumer หรือ business job handler
`/health` ของ API ตรวจ process ส่วน RabbitMQ service, vhost, users, permissions และ queues ต้องเตรียมไว้ภายนอกแอป
`db:migrate` สร้างบัญชี runtime ที่ยังไม่มีด้วยรหัสผ่านใน config โดยใช้ admin connection; บัญชีที่มีอยู่แล้วจะไม่ถูกเปลี่ยนรหัสผ่านหรือสิทธิ์ระดับ role

## Build ผ่าน Nx

ใช้ Node.js 24, npm, .NET SDK 9.0.3xx ตาม `global.json`, Flutter พร้อม Android SDK
เรียกจากโฟลเดอร์ `project/`:

```sh
npm ci
npm run build
```

`build` เรียก Core API, Worker, React + Vite และ Android APK

คำสั่งรายส่วน:

```sh
npm run build:api
npm run build:web
npm run build:mobile
npm run build:ios
npm run docker:build
```

iOS build ต้องใช้ macOS + Xcode; target นี้เป็น unsigned build และยังต้อง signing ก่อนเผยแพร่
Android release scaffold ใช้ signing ตัวอย่างของ Flutter ต้องตั้ง signing จริงก่อนเผยแพร่
Dockerfiles มีเฉพาะ Core API, Worker และ Web

## Run dev / start

เรียกจากโฟลเดอร์ `project/` โดยผู้ใช้เป็นผู้เริ่มแอปเอง:

| ส่วน | Development / Hot Reload | Release / Production |
| --- | --- | --- |
| Core API + Background Worker | `npm run dev:api` | `npm run start:api` |
| Web | `npm run dev:web` | `npm run start:web` |
| Android | `npm run dev:mobile` | `npm run start:mobile` |
| iOS | `npm run dev:ios` | `npm run start:ios` |

Web ใช้ Vite สำหรับ development; `npm run build:web` ตรวจ TypeScript แล้ว build ไฟล์ static ไปที่ `dist/apps/web`
`npm run start:web` build ผ่าน Nx ก่อนใช้ `serve` ให้บริการไฟล์ static พร้อม SPA fallback
Docker ใช้ไฟล์ build และ server เดียวกันที่พอร์ต 3000 จึงใช้ Compose เดิมได้
Web เป็น client-side SPA ไม่มี SSR หรือ Next.js API routes; ลบ route ตัวอย่าง `/api/hello` แล้ว
API จริงยังอยู่ใน Core API .NET ที่พอร์ต 3100 และยังไม่ได้เพิ่มการเชื่อม API ในหน้าเว็บ
เมื่อเพิ่ม client configuration ให้ใช้ชื่อ `VITE_*` เฉพาะค่าที่เปิดเผยได้ เพราะค่าเหล่านี้ถูกฝังในไฟล์ build
ห้ามนำ DB/Queue credentials ไปใส่ในตัวแปร `VITE_*`

กำหนดพอร์ตใน `.env`:

```dotenv
WEB_PORT=3000
API_PORT=3100
```

คำสั่ง Nx dev/start บน Linux/macOS อ่านค่าพอร์ตเหล่านี้ และใช้ค่า 3000/3100 หากยังไม่ได้กำหนด
Mobile ใช้ค่า default ของ Flutter; Background Worker ไม่มี HTTP port ของตนเอง
Worker ใช้ URL ของ API ตาม `API_PORT` เว้นแต่กำหนด `Core__BaseUrl` ไว้เอง

`dev:api` รัน Core API, BU Worker และ Ingest Worker พร้อมกันผ่าน `dotnet watch` ใน Development
BU Worker ใช้ค่า BU01 จาก `appsettings.Development.json` เป็นค่าเริ่มต้น เปลี่ยนสาขาได้ด้วย `BU_ID`
เมื่อไม่ได้กำหนด `ConnectionStrings:Bu` ของ worker เอง จะใช้ `<BU_ID>_DB_CONNECTION` และ runtime password จาก `.env` ก่อน fallback ไป `ConnectionStrings:<BU_ID>` ใน worker หรือไฟล์ `apps/api/core-api/api/appsettings.Local.json`
RabbitMQ ใช้ URI ที่กำหนดใน `Queue:ConnectionString` หรือ `<BU_ID>_QUEUE_CONNECTION` โดยตรง รวมทั้ง host, port, username, password และ vhost
ก่อนเริ่ม API/worker ครั้งแรกให้รัน `npm run db:migrate` แล้ว `npm run db:seed` ตามลำดับ
ค่าพอร์ตเริ่มต้น: API อยู่ที่ `http://localhost:3100`; Web อยู่ที่ `http://localhost:3000`

`start:api` รันทั้งสาม process ใน Production หลัง build Release ผ่าน Nx และไม่ใช้ launch profile
ต้องกำหนด `Bu__Id` และ `Bu__QueueName` ให้ตรงกันก่อนรัน เพราะ Production ไม่โหลด BU01 จากไฟล์ Development
ตัวอย่างสำหรับ BU01 บน Linux/macOS:

```sh
Bu__Id=BU01 Bu__QueueName=jobs.bu01 npm run start:api
```

คำสั่ง API เริ่ม BU Worker เพียงหนึ่ง instance ตาม BU config
BU Worker ตรวจการเชื่อม DB และการมีอยู่ของ queue โดยยังไม่รับข้อความหรือประมวลผลงานจริง
`start:web` build ผ่าน Nx แล้วเตรียม static/public assets และใช้ standalone server ตาม config ปัจจุบัน

คำสั่ง Mobile เลือกเฉพาะอุปกรณ์ Android ส่วนคำสั่ง iOS เลือกเฉพาะอุปกรณ์ iOS
เมื่อมีอุปกรณ์ที่ใช้ได้หนึ่งเครื่องจะเลือกให้อัตโนมัติ หากมีหลายเครื่องให้ระบุ ID จาก `flutter devices`:

```sh
npm run dev:mobile -- --device-id=emulator-5554
npm run dev:ios -- --device-id="IOS_DEVICE_ID"
```

แทน `IOS_DEVICE_ID` ด้วย ID จริง; เปิด emulator/simulator หรือเชื่อมต่ออุปกรณ์เองก่อนรัน
`dev:mobile` และ `dev:ios` ใช้ Flutter debug mode และกด `r` เพื่อ Hot Reload ได้
`start:mobile` และ `start:ios` ใช้ release mode ต้องใช้อุปกรณ์จริง ไม่ใช้ emulator/simulator
ทั้งสองคำสั่ง iOS ต้องรันบน macOS ที่มี Xcode; อุปกรณ์จริงต้องตั้ง signing จึงรันไม่ได้บน Linux เครื่องนี้

## Docker Compose

1. กำหนด `.env` ให้ทุก endpoint/credential ตรงระบบทดลอง
2. Build images: `npm run docker:build`
3. ผู้ใช้เริ่ม container เอง: `docker compose up -d`

Compose สร้าง Core API 1 container, Web 1 container และ Workers 3 containers จาก Worker image เดียว
ไม่มี Database/Queue containers; connection settings เตรียมไว้สำหรับ client/consumer ที่จะพัฒนาต่อ
DB ใช้ปลายทางภายนอก ส่วน RabbitMQ ต้องกำหนด host ให้ containers เข้าถึง service บนเครื่องได้; `localhost` ใน container หมายถึง container นั้นเอง
ค่าพอร์ตเริ่มต้น: Web `http://localhost:3000`; API health `http://localhost:3100/health`
`WEB_PORT` และ `API_PORT` กำหนด host ports ของ Compose; ภายใน container ใช้ Web 3000 และ API 3100
ค่า `.env` ไม่ถูก commit และไม่ถูกส่งเข้า Docker build context

กำหนดรหัสผ่าน app user ของ Core และแต่ละ BU ใน `.env`:

```dotenv
CORE_DB_PASSWORD=REPLACE_ME
BU01_DB_PASSWORD=REPLACE_ME
BU02_DB_PASSWORD=REPLACE_ME
```

เปลี่ยนเป็นรหัสผ่านจริงคนละชุด เช่น random hex 64 ตัวอักษรที่สร้างด้วย `openssl rand -hex 32`
`CORE_DB_CONNECTION` และ `<BU_ID>_DB_CONNECTION` เก็บ Host/Database/Username โดยไม่ใส่ Password
Compose จะเติม Password จาก env แยกให้เอง; รหัสผ่านต้องตรงกับ app user ใน DB ภายนอกที่เตรียมไว้
การแก้ env ไม่ได้เปลี่ยนรหัสผ่านใน DB ที่มีอยู่แล้ว
`CORE_QUEUE_CONNECTION` และ `BUxx_QUEUE_CONNECTION` ใช้ user/password ของ broker แยกจาก DB
จึงต้องตั้งบัญชีและสิทธิ์ให้ตรงกับ Queue จริง ไม่ได้ดึงค่า `DB_PASSWORD` ไปใช้โดยอัตโนมัติ

## การตั้งค่า DB และ Queue

### ตั้งค่า DB ภายนอก

กำหนด `CORE_DB_CONNECTION` และ `<BU_ID>_DB_CONNECTION` ใน `.env` ให้เป็น Host/Port/Database ของฐานที่ผู้ให้บริการสร้างไว้แล้ว ตัวอย่าง:

```dotenv
CORE_DB_CONNECTION="Host=db.example.invalid;Port=5433;Database=core;Username=core_user"
BU01_DB_CONNECTION="Host=db.example.invalid;Port=5434;Database=bu01;Username=bu01user"
BU02_DB_CONNECTION="Host=db.example.invalid;Port=5435;Database=bu02;Username=bu02user"
```

- `*_DB_PASSWORD`: รหัสผ่านบัญชี runtime ของ API/Worker แยกจาก admin
- `*_DB_ADMIN_PASSWORD`: รหัสผ่านผู้ดูแลสำหรับ migrate; admin user เริ่มต้นเป็น `postgres` หรือกำหนด `*_DB_ADMIN_USER`
- `<BU_ID>_HOSPITAL_ID`: รหัสโรงพยาบาล; ถ้าไม่กำหนดใช้ BU ID เดิม
- `*_DB_SCHEMA`: ค่าเริ่มต้น `core` สำหรับ Core และ `bu` สำหรับ BU
- `ConnectionStrings` ใน `apps/api/core-api/api/appsettings.Local.json` ต้องชี้ Host/Port/Database/User เดียวกัน เช่น `Core`, `CoreMigration`, `Bu01`, `Bu01Migration`; runtime และ migration ใช้คนละบัญชี

`db:migrate` สร้าง runtime LOGIN role ที่ยังไม่มีพร้อมสิทธิ์ของแอป และเตรียม schema ให้ Core/ทุก BU ที่ตั้งค่าไว้ ต้องมี admin ที่สร้าง role และ schema ได้ บัญชี runtime ที่มีอยู่แล้วต้องเป็น LOGIN และไม่มี SUPERUSER/BYPASSRLS; migration จะไม่เปลี่ยนรหัสผ่านบัญชีเดิม ไม่สร้าง/เปลี่ยนชื่อ database และไม่ล้าง business data

### ตั้งค่า RabbitMQ

ใช้ RabbitMQ service ที่มีอยู่ที่ `localhost:5672` พร้อม vhost `ida`, บัญชี `core_publisher`, `bu01_worker`, `bu02_worker` และ queue `jobs.bu01`, `jobs.bu02` ที่เตรียมไว้แล้ว ตัวอย่าง URI ใน `.env`:

```dotenv
CORE_QUEUE_CONNECTION="amqp://core_publisher:REPLACE_ME_CORE_QUEUE_PASSWORD@localhost:5672/ida"
BU01_QUEUE_CONNECTION="amqp://bu01_worker:REPLACE_ME_BU01_QUEUE_PASSWORD@localhost:5672/ida"
BU02_QUEUE_CONNECTION="amqp://bu02_worker:REPLACE_ME_BU02_QUEUE_PASSWORD@localhost:5672/ida"
```

แทนรหัสผ่านตัวอย่างด้วยรหัสผ่านบัญชี broker ที่มีอยู่และกำหนดสิทธิ์ให้ตรงกับ queue ของแต่ละ BU แอปใช้ connection ที่ตั้งค่าไว้และไม่ติดตั้ง broker หรือสร้าง vhost, users, exchange, queue และ binding ให้

### เริ่มใช้งาน

เตรียม `.env`/API local settings ให้ตรงกับ PostgreSQL ภายนอกและ RabbitMQ ที่มีอยู่ แล้วรัน:

```sh
npm run db:migrate
npm run db:seed
npm run dev:api
```

เปิดอีก terminal แล้วรัน `npm run dev:web` ฐานข้อมูลใช้ปลายทางภายนอกโดยตรง ส่วน BU Worker เชื่อม RabbitMQ ตาม URI ที่กำหนด ค่าเริ่มต้นเลือก BU01 และเปลี่ยนสาขาด้วย `BU_ID`

### เครือข่ายและการเพิ่ม BU

เครื่องที่รัน API/migrate ต้องเข้าถึง PostgreSQL ตาม Host/Port ที่ตั้งไว้ Core API ใช้ Core และทุก BU ส่วน Worker ใช้เฉพาะ DB ของ BU ที่เลือก

เพิ่ม BU โดยเตรียม database ภายนอกและบัญชี/queue ของ RabbitMQ แล้วเพิ่ม `<BU_ID>_DB_CONNECTION`, DB password, admin settings และ queue connection ใน `.env` หากมี connection ใน API local settings ให้เพิ่มให้ตรงกันด้วย จากนั้นรัน migrate/seed และเลือก BU ด้วย runtime configuration

## Claude / Codex

มีเฉพาะ Claude/Codex configuration และ Nx skills:
`nx-workspace`, `nx-generate`, `nx-run-tasks`, `nx-plugins`, `nx-import`
ยังไม่เพิ่ม skills ของ .NET, Flutter หรือ React และไม่มี Nx Cloud/CI monitor อัตโนมัติ

## References

- [Nx custom commands](https://nx.dev/docs/reference/nx/executors)
- [RabbitMQ definitions import](https://www.rabbitmq.com/docs/definitions)
- [RabbitMQ password hashing](https://www.rabbitmq.com/docs/passwords)
- [RabbitMQ access control](https://www.rabbitmq.com/docs/access-control)
- [Docker Compose environment interpolation](https://docs.docker.com/compose/how-tos/environment-variables/variable-interpolation/)
- [Node.js environment file parsing](https://nodejs.org/api/environment_variables.html)
- [Nx Vite plugin](https://nx.dev/docs/technologies/build-tools/vite/introduction)
- [React application setup](https://react.dev/learn/build-a-react-app-from-scratch)
- [Vite static deployment](https://vite.dev/guide/static-deploy.html)
- [.NET watch](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-watch)
- [Flutter build modes](https://docs.flutter.dev/testing/build-modes)
- [Flutter iOS setup](https://docs.flutter.dev/platform-integration/ios/setup)
