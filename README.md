# iDA — Nx monorepo

โครงเริ่มต้นสำหรับ Flutter (Android/iOS), Next.js และ backend .NET 9 ภายใต้ Nx

ชื่อแสดงผลของระบบคือ `iDA`; package scope, Docker images, Kubernetes labels และ RabbitMQ vhost ใช้ `ida`
ฐานข้อมูลใน config ใช้ `ida_core` และ `ida_bu01`–`ida_bu03` โดยยังไม่เปลี่ยน DB หรือข้อมูลบน PVC ที่มีอยู่
PostgreSQL initialize ชื่อ DB เฉพาะ volume ว่าง; หากใช้ PVC เดิมต้องจัดการ migration/rename DB ก่อน deploy config ใหม่นี้
RabbitMQ vhost ใหม่แยกจาก vhost เดิม และไม่ได้ย้าย messages/objects ให้เอง
ก่อนใช้ `kube:purge` กับ cluster ที่ deploy ไว้เดิม ต้องมี namespace labels `ida.io/role`/`ida.io/bu` ตาม manifests ใหม่
คำสั่ง purge ไม่ค้นหา labels เดิม; การเปลี่ยนชื่อในไฟล์ไม่ deploy, restart หรือลบทรัพยากรใน cluster
สร้าง images ใหม่ในชื่อ `ida/*` และเตรียม Secrets/config ให้ตรงกันก่อน apply โดยไม่ลบ PVC เพื่อเปลี่ยนชื่อ
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
│   ├── web/                    Next.js frontend กลาง
│   └── api/                    backend solution กลาง
│       ├── iDA.sln
│       ├── core-api/           Core API กลางหนึ่ง service
│       └── background-worker/  งานประมวลผลเบื้องหลัง ใช้ source/image ร่วมกันทุก BU
├── infrastructure/
│   └── kubernetes/
│       ├── core/               Core API + Web + Core PostgreSQL + RabbitMQ
│       ├── base/worker/        Worker Deployment template กลาง
│       ├── base/database/      PostgreSQL StatefulSet + Service + PVC ต่อ BU
│       └── branches/bu01…bu03/ config/namespace/secret example เท่านั้น
├── .agents/skills/             เฉพาะ Nx skills สำหรับ Codex
├── .claude/skills/             symlink ไป Nx skills ชุดเดียวกัน
├── .codex/config.toml          Nx MCP สำหรับ Codex
├── .mcp.json                   Nx MCP สำหรับ Claude
├── compose.yaml
├── .env.example
├── nx.json
└── package.json
```

## Backend และ BU

Backend อยู่ใน solution เดียว มีสอง process roles: Core API และ Worker
ไม่มี source หรือ project ที่คัดลอกแยกเป็น BU01–BU03

- Core API กลางเชื่อม **Core Database 1 ก้อน** และ publish งานเข้า Queue/Orchestrator
- Worker ทั้ง 3 BU ใช้ image `ida/bu-worker` เดียวกัน เปลี่ยนเฉพาะ runtime configuration
- Kubernetes สร้าง Worker Deployment 1 ตัว + PostgreSQL StatefulSet 1 ตัว ในแต่ละ namespace `bu01`–`bu03`
- `Bu__Id=BU01` คู่กับ `Bu__QueueName=jobs.bu01` และ `ConnectionStrings__Bu` ของ BU01
- BU Database แยก 3 ก้อน: `ida_bu01`–`ida_bu03` แต่ละก้อนมี container, user และ PVC ของตนเอง
- Worker เข้าข้อมูลกลางผ่าน Core API; ไม่ได้รับ credential ของ Core DB
- RabbitMQ กลางใน namespace `core` แยก queue และ ACL/credential ต่อ BU
- Web/Mobile ใช้งานร่วมกันทุก BU; Mobile เป็น client บนอุปกรณ์

`background-worker` คือโปรเจกต์งานประมวลผลเบื้องหลังที่ใช้ร่วมกันทุกสาขา ไม่ใช่ source แยกต่อสาขา
เพิ่ม replicas ได้ภายหลังเมื่อทำการ claim งาน, idempotency และ concurrency control แล้ว

## สถานะ scaffold

| ส่วน | มีแล้ว | ยังต้องทำต่อ |
| --- | --- | --- |
| Web | Next.js starter + standalone Dockerfile | หน้าใช้งานและ API integration |
| Mobile | Flutter starter, Android และ iOS/Swift | หน้าจอและ API integration |
| Core API | .NET 9, `GET /health`, OpenAPI ใน Development | DB client, authentication, queue publisher/orchestrator, business endpoints |
| Worker | อ่าน/ตรวจ BU config, รองรับ shutdown, Dockerfile | queue consumer, BU DB client, ตรวจ tenant ของ message, retry/DLQ, business jobs |
| Docker | Compose สำหรับ Core API, Web และ 3 Workers | ตั้งปลายทาง/credential และ build images |
| Kubernetes | Core + 3 BU namespaces, Workers 3 ตัว, PostgreSQL 4 ตัว, RabbitMQ กลาง, PVC, Secrets references, NetworkPolicy | เตรียม images, Secrets, storage และตรวจว่า CNI รองรับ NetworkPolicy |

**ยังไม่ใช่ระบบประมวลผลงานครบวงจร** Worker จะ log ว่าเป็น scaffold แล้วรอ shutdown
ยังไม่รับ Queue, เรียก Core API หรือเขียน DB; `/health` ตรวจเฉพาะ process ไม่ได้ตรวจ DB/Queue

Kubernetes สร้าง PostgreSQL และ app user ของ Core/แต่ละ BU เมื่อเริ่มด้วย volume ว่าง แต่ยังไม่สร้าง business tables/migrations
RabbitMQ สร้าง vhost, users, permissions, exchange และ queues จาก Secret ที่เตรียมด้วย `kube:secrets`
มี broker แล้ว แต่ logic publisher/orchestrator และ consumer ในแอปยังต้องพัฒนาต่อ
Docker Compose ยังคงใช้ Database/Queue ภายนอกทั้งหมด ไม่ได้เพิ่ม DB containers ใน Compose
ตัวอย่าง Kubernetes ใช้ PostgreSQL และ AMQP/RabbitMQ ภายใน cluster แบบไม่มี TLS สำหรับทดลองเท่านั้น
ยังไม่ได้ติดตั้ง client libraries ในแอป

## Build ผ่าน Nx

ใช้ Node.js 24, npm, .NET SDK 9.0.3xx ตาม `global.json`, Flutter พร้อม Android SDK และ kubectl
เรียกจากโฟลเดอร์ `project/`:

```sh
npm ci
npm run build
```

`build` เรียก Core API, Worker, Next.js, Android APK และ render Kubernetes manifests
ไปที่ `infrastructure/rendered.yaml`; การ render ใช้ไฟล์ local ไม่ติดต่อ/แก้ cluster

คำสั่งรายส่วน:

```sh
npm run build:api
npm run build:web
npm run build:mobile
npm run build:ios
npm run docker:build
npm run kube:render
```

iOS build ต้องใช้ macOS + Xcode; target นี้เป็น unsigned build และยังต้อง signing ก่อนเผยแพร่
Android release scaffold ใช้ signing ตัวอย่างของ Flutter ต้องตั้ง signing จริงก่อนเผยแพร่
Dockerfiles มีเฉพาะ Core API, Worker และ Web; Mobile ไม่ deploy เข้า Kubernetes

## Run dev / start

เรียกจากโฟลเดอร์ `project/` โดยผู้ใช้เป็นผู้เริ่มแอปเอง:

| ส่วน | Development / Hot Reload | Release / Production |
| --- | --- | --- |
| Core API + Background Worker | `npm run dev:api` | `npm run start:api` |
| Web | `npm run dev:web` | `npm run start:web` |
| Android | `npm run dev:mobile` | `npm run start:mobile` |
| iOS | `npm run dev:ios` | `npm run start:ios` |

กำหนดพอร์ตใน `.env` โดยใช้ `.env.example` เป็นตัวอย่าง:

```dotenv
WEB_PORT=3000
API_PORT=3100
```

คำสั่ง Nx dev/start บน Linux/macOS อ่านค่าพอร์ตเหล่านี้ และใช้ค่า 3000/3100 หากยังไม่ได้กำหนด
Mobile ใช้ค่า default ของ Flutter; Background Worker ไม่มี HTTP port ของตนเอง
Worker ใช้ URL ของ API ตาม `API_PORT` เว้นแต่กำหนด `Core__BaseUrl` ไว้เอง

`dev:api` รัน Core API และ Worker พร้อมกันผ่าน `dotnet watch` ใน Development
Worker ใช้ค่า BU01 จาก `appsettings.Development.json` เป็นค่าเริ่มต้นของ scaffold
ค่าพอร์ตเริ่มต้น: API อยู่ที่ `http://localhost:3100`; Web อยู่ที่ `http://localhost:3000`

`start:api` รันทั้งสอง process ใน Production หลัง build Release ผ่าน Nx และไม่ใช้ launch profile
ต้องกำหนด `Bu__Id` และ `Bu__QueueName` ให้ตรงกันก่อนรัน เพราะ Production ไม่โหลด BU01 จากไฟล์ Development
ตัวอย่างสำหรับ BU01 บน Linux/macOS:

```sh
Bu__Id=BU01 Bu__QueueName=jobs.bu01 npm run start:api
```

คำสั่ง API เริ่ม Worker เพียงหนึ่ง instance ตาม BU config ไม่ได้เริ่ม Workers ทั้ง 3 ตัว
Worker ยังเป็น scaffold ที่รอเฉย ๆ ยังไม่รับ Queue หรือประมวลผลข้อมูลจริง
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

1. คัดลอก `.env.example` เป็น `.env` แล้วเปลี่ยนทุก endpoint/credential ให้ตรงระบบทดลอง
2. Build images: `npm run docker:build`
3. ผู้ใช้เริ่ม container เอง: `docker compose up -d`

Compose สร้าง Core API 1 container, Web 1 container และ Workers 3 containers จาก Worker image เดียว
ไม่มี Database/Queue containers; connection settings เตรียมไว้สำหรับ client/consumer ที่จะพัฒนาต่อ
ค่าเริ่มต้นใน `.env.example` เป็น DNS ภายใน Kubernetes ซึ่ง Compose/local โดยทั่วไปเข้าถึงไม่ได้
หากใช้ Compose ต้องเปลี่ยน endpoints ให้เข้าถึงได้จาก containers; การใช้ `.env` สำหรับ Kubernetes ให้คง endpoints ภายใน cluster
ค่าพอร์ตเริ่มต้น: Web `http://localhost:3000`; API health `http://localhost:3100/health`
`WEB_PORT` และ `API_PORT` กำหนด host ports ของ Compose; ภายใน container ใช้ Web 3000 และ API 3100
ค่า `.env` ไม่ถูก commit และไม่ถูกส่งเข้า Docker build context

กำหนดรหัสผ่าน app user ของ Core และแต่ละ BU ใน `.env`:

```dotenv
CORE_DB_PASSWORD=REPLACE_ME
BU01_DB_PASSWORD=REPLACE_ME
BU02_DB_PASSWORD=REPLACE_ME
BU03_DB_PASSWORD=REPLACE_ME
```

เปลี่ยนเป็นรหัสผ่านจริงคนละชุด เช่น random hex 64 ตัวอักษรที่สร้างด้วย `openssl rand -hex 32`
`CORE_DB_CONNECTION` และ `BU01_DB_CONNECTION`–`BU03_DB_CONNECTION` เก็บ Host/Database/Username โดยไม่ใส่ Password
Compose จะเติม Password จาก env แยกให้เอง; รหัสผ่านต้องตรงกับ app user ใน DB ภายนอกที่เตรียมไว้
การแก้ env ไม่ได้เปลี่ยนรหัสผ่านใน DB ที่มีอยู่แล้ว
`CORE_QUEUE_CONNECTION` และ `BUxx_QUEUE_CONNECTION` ใช้ user/password ของ broker แยกจาก DB
จึงต้องตั้งบัญชีและสิทธิ์ให้ตรงกับ Queue จริง ไม่ได้ดึงค่า `DB_PASSWORD` ไปใช้โดยอัตโนมัติ
รหัสผ่านที่ใส่ใน `.env.example` เป็นตัวอย่างสำหรับทดลอง ไม่ใช่ค่าพร้อมใช้งานจริง

## Kubernetes

Service/container ports ใช้ Web 3000 และ API 3100; Kubernetes manifests ไม่อ่าน `.env` โดยตรง
ใช้ `npm run kube:secrets` เพื่ออ่าน credential จาก `.env` แล้วสร้าง/อัปเดต Secrets ก่อน deploy

หลังเตรียมระบบและ Secrets ครั้งแรกแล้ว deploy ทั้งชุดจากโฟลเดอร์ `project/` ด้วยคำสั่งเดียว:

```sh
npm run kube:apply
```

คำสั่งนี้เรียก `kubectl apply -k infrastructure/kubernetes` โดยสร้าง/อัปเดต Core API, Web, Core DB และ RabbitMQ กลาง
แล้วแสดงข้อมูลเชื่อมต่อฐานข้อมูลทุก BU จาก cluster โดยไม่รอให้ Pods พร้อมใช้งาน
พร้อม namespace `bu01`–`bu03` ซึ่งแต่ละ BU มี:

- Worker Deployment 1 replica ใช้ Worker image เดียวกันทั้ง 3 BU
- PostgreSQL `bu-db` 1 replica มี database `ida_buNN` และ app user `bu01user`–`bu03user` ที่ไม่ใช่ superuser
- Service `bu-db` พอร์ต 5432 และ PVC `data-bu-db-0` ขนาด 10Gi แยกตาม namespace
- Service `bu-db-client` แบบ ClusterIP สำหรับโปรแกรมเชื่อมฐานข้อมูล มี IP ของตนเองในแต่ละ namespace
- NetworkPolicy อนุญาตเฉพาะ Worker ใน namespace เดียวกันเข้า DB ของ BU นั้น

Worker ใช้ `Host=bu-db` ซึ่ง resolve ไปยัง DB ใน namespace ของตนเอง
ชื่อ database/user อ่านจาก `database-config`; password อ่านจาก `worker-secrets.Database__Password`
ซึ่งคำสั่ง `kube:secrets` นำมาจาก `BU01_DB_PASSWORD`–`BU03_DB_PASSWORD` ใน `.env`
แล้วประกอบ `ConnectionStrings__Bu` ใน Deployment โดยใช้ password แหล่งเดียวกับที่สร้าง app user
รหัสผ่านผู้ดูแล PostgreSQL อยู่ใน `database-secrets` และไม่ได้ส่งให้ Worker
Core ใช้ `CORE_DB_CONNECTION` รวมกับ `CORE_DB_PASSWORD` เพื่อสร้าง `core-secrets.ConnectionStrings__Core`
Core DB ใช้ app user `core_app` และ database `ida_core`; app password มาจาก `CORE_DB_PASSWORD` ค่าเดียวกัน
`CORE_DB_ADMIN_PASSWORD` ใช้กับผู้ดูแล PostgreSQL เท่านั้น ไม่ได้ส่งให้ Core API
ยังไม่มีการเปิดให้ Core API query BU DB โดยตรง; ต้องเพิ่ม client, connection routing และสิทธิ์เมื่อพัฒนาส่วนนั้น

### การเชื่อมต่อฐานข้อมูล BU จากเครื่อง Node

ทุก BU ที่ใช้ `base/database` มี Service `bu-db-client` แบบ ClusterIP สำหรับโปรแกรมเชื่อมฐานข้อมูล เช่น DBeaver
Service เลือกเฉพาะ `app: bu-db` ใน namespace ของตนเอง จึงเชื่อมไปยัง DB ของ BU นั้นเท่านั้น
แต่ละ BU มี DB container, บัญชี และ PVC แยกกันตามเดิม; Service `bu-db` ยังคงเป็น headless สำหรับ Worker และ StatefulSet
ชื่อ resource ไม่ผูกกับโปรแกรม client และเมื่อเพิ่ม BU ที่ใช้ base เดียวกันจะได้ Service นี้ด้วย ไม่ต้องแก้รายชื่อ BU ใน script

รันจากโฟลเดอร์ `project/` หลังเตรียม images, storage และ `.env` แล้ว:

```sh
npm run kube:render
npm run kube:secrets
npm run kube:apply
```

หลัง apply จะมีตาราง BU, namespace, Host, Port, Database, Username และสถานะ readiness
รายชื่อ BU อ่านจาก Namespace manifests; database/user อ่านจาก ConfigMap ที่ StatefulSet ใช้จริง
หากต้องการดูข้อมูลล่าสุดอีกครั้ง ใช้คำสั่ง read-only:

```sh
npm run kube:db-info
```

สร้าง PostgreSQL connection แยกกันสำหรับแต่ละ BU ใน DBeaver หรือโปรแกรม client อื่น:

| BU | Host | Port | Database | Username | Password |
| --- | --- | --- | --- | --- | --- |
| BU01 | ClusterIP ของ BU01 จากตาราง | `5432` | `ida_bu01` | `bu01user` | รหัสที่ใช้สร้าง app user จาก `BU01_DB_PASSWORD` |
| BU02 | ClusterIP ของ BU02 จากตาราง | `5432` | `ida_bu02` | `bu02user` | รหัสที่ใช้สร้าง app user จาก `BU02_DB_PASSWORD` |
| BU03 | ClusterIP ของ BU03 จากตาราง | `5432` | `ida_bu03` | `bu03user` | รหัสที่ใช้สร้าง app user จาก `BU03_DB_PASSWORD` |

ใช้ค่า Host/Port/Database/Username ที่แสดงจริงจาก script หากแก้ config ไม่ใช้ IP ของ Node หรือ `localhost`
พอร์ตเหมือนกันได้เพราะแต่ละ BU มี IP ของ Service คนละตัว; script ไม่อ่าน Secrets หรือพิมพ์รหัสผ่าน
ต้องให้ DB พร้อมใช้งานก่อนเชื่อม; readiness ที่แสดงไม่ใช่การทดสอบ username/password หรือ SQL
สำหรับ client บนเครื่อง Node เดียวกับ DB Pod ไม่ต้องใช้ port-forward หรือเปิด NodePort; ไม่มีการเปิดพอร์ตรับจาก LAN เพิ่ม
NetworkPolicy เดิมไม่ถูกแก้ เพราะการเข้าถึงจาก Node ที่ DB Pod รันอยู่ได้รับอนุญาต
หากมี host firewall เพิ่มเติมหรือ client ไม่ได้อยู่บน Node นั้น ต้องตรวจเส้นทางและสิทธิ์ก่อนเชื่อม
ClusterIP คงเดิมตราบที่ Service ยังอยู่; `kube:purge` ลบ Services นี้พร้อม namespace และ IP อาจเปลี่ยนเมื่อสร้างใหม่
`kube:apply` ไม่เตรียม images หรือ storage ให้อัตโนมัติ; สำหรับเครื่อง Node เดียวใช้ `kube:images` และ `kube:storage` ตามขั้นตอนด้านล่าง
ถ้า DB ยัง `Pending` จะยังเชื่อมไม่ได้
Service ชื่อเก่าที่เคยสร้างเองจะไม่ถูก prune หรือลบโดย `kube:apply`; การเปลี่ยนนี้ไม่ลบ DB/PVC เดิม

| คำสั่ง | หน้าที่ |
| --- | --- |
| `npm run kube:render` | แสดง manifests รวม Core และทุก BU โดยไม่ deploy |
| `npm run kube:secrets` | สร้าง/อัปเดต namespaces และ Secrets จาก `.env` |
| `npm run kube:images` | Build app images แล้ว import เข้า containerd ของ Node เครื่องนี้ |
| `npm run kube:storage` | สร้างโฟลเดอร์และ Local PV สำหรับ PVC ที่ Pending ของ StatefulSets ใน manifests |
| `npm run kube:apply` | Apply workloads และ Services แล้วแสดงข้อมูลเชื่อมต่อทุก BU |
| `npm run kube:db-info` | อ่านข้อมูลเชื่อมต่อและสถานะ DB ล่าสุด ไม่เปลี่ยน cluster |
| `npm run kube:purge` | ลบ namespaces ของระบบ รวม workloads, Services, Secrets และ PVC; ไม่ใช้เพื่ออัปเดต |

### Core DB และ RabbitMQ ภายใน cluster

| ส่วน | Service DNS | พอร์ต | ข้อมูลถาวร |
| --- | --- | --- | --- |
| Core PostgreSQL | `core-db.core.svc.cluster.local` | 5432 | PVC 10Gi |
| BU01 PostgreSQL | `bu-db.bu01.svc.cluster.local` | 5432 | PVC 10Gi |
| BU02 PostgreSQL | `bu-db.bu02.svc.cluster.local` | 5432 | PVC 10Gi |
| BU03 PostgreSQL | `bu-db.bu03.svc.cluster.local` | 5432 | PVC 10Gi |
| RabbitMQ | `queue.core.svc.cluster.local` | 5672 | PVC 5Gi |

RabbitMQ ใช้ vhost `ida` และ direct exchange `jobs` โดยเตรียม durable classic queues
`jobs.bu01`, `jobs.bu02`, `jobs.bu03` พร้อม routing key ที่ตรงกับชื่อ queue
`kube:secrets` อ่านบัญชีจาก `CORE_QUEUE_CONNECTION` และ `BUxx_QUEUE_CONNECTION`
แล้วสร้าง password hashes และ definitions ใน Secret `queue-definitions` ไม่เขียน credentials ลง rendered manifests

- บัญชี Core publish ได้เฉพาะ exchange `jobs`; ส่งงานไป BU ใดให้ใช้ routing key ของ BU นั้น
- บัญชี Worker อ่านได้เฉพาะ queue ของ BU ตัวเอง ไม่สามารถ publish, สร้าง/ลบ queue หรืออ่านข้าม BU
- แอปที่พัฒนาต่อต้องใช้ topology ที่เตรียมไว้ ไม่ declare/bind resources ใหม่ด้วยบัญชีเหล่านี้
- URL ใน `.env` ต้องเป็น `amqp://USER:PASSWORD@queue.core.svc.cluster.local:5672/ida`
  ใช้ username คนละชื่อระหว่าง Core/แต่ละ BU และ percent-encode อักขระพิเศษใน user/password

NetworkPolicy อนุญาต Core API เข้า Core DB และอนุญาต Core API/BU Workers เข้า RabbitMQ
ไม่มีการเปิด PostgreSQL, RabbitMQ หรือ management UI ออกภายนอก cluster

### เตรียมก่อน deploy

1. Build/push images ของ Core API, Worker และ Web ไป registry ใช้ version tag/digest เดียวสำหรับ Workers ทั้ง 3 BU
   เปลี่ยน image references ใน manifests หรือใช้ `npm run kube:images` สำหรับ local containerd cluster เครื่องนี้
2. Cluster ต้องมี default StorageClass ที่ provision PVC แบบ ReadWriteOnce ได้ หรือเตรียม PersistentVolumes ให้ตรงกัน
   สำหรับ PVC เดิมที่ Pending และยังไม่มี StorageClass ใช้ `npm run kube:storage` หลัง apply เพื่อเตรียม Static Local PV
   ค่าเริ่มต้นขอ storage รวม 45Gi สำหรับ DB 4 ก้อนและ RabbitMQ และต้องมี CPU/RAM พอสำหรับ workloads ทั้งชุด
3. Cluster ต้องเข้าถึง images `postgres:17-bookworm` และ `rabbitmq:4.2.9` หรือ mirror images ไว้เอง
4. ปรับ DNS selector/domain ให้ตรง cluster; ตัวอย่างใช้ `cluster.local` และ kube-dns
   PostgreSQL ใช้ 5432 และ RabbitMQ ใช้ AMQP 5672 แบบไม่มี TLS
5. เตรียม Secrets ตามด้านล่าง แล้วผู้ใช้จึงรัน `npm run kube:apply` เอง

ไม่ต้องเตรียม Core DB/Queue ภายนอกหรือแก้ external IP allowlist สำหรับชุด Kubernetes นี้
NetworkPolicy ต้องมี CNI ที่รองรับจึงจะบังคับ isolation ได้จริง
ไม่มี password จริงใน manifests และ `npm run build`/`npm run kube:render` ไม่สร้าง Secrets หรือ deploy เข้า cluster

### Local storage และ images สำหรับ Node เครื่องนี้

กำหนดไว้ใน `.env` และ `.env.example`:

```dotenv
KUBE_STORAGE_ROOT=/var/lib/ida/storage
```

สำหรับระบบที่ apply แล้วและ DB ยัง Pending ให้รันจาก `project/`:

```sh
npm run kube:storage
npm run kube:images
npm run kube:db-info
```

`kube:storage` อ่าน StatefulSets จาก manifests และขนาด/ชื่อ PVC ที่ deploy จริง ไม่ฝังรายชื่อ BU
ตรวจว่าเป็น Linux cluster ที่มี Node เดียว พร้อมใช้งาน และ hostname ตรงกับเครื่องที่รันคำสั่ง
สร้างเฉพาะ Local PV สำหรับ PVC ที่ Pending และไม่มี StorageClass โดยจองกับ namespace/name/UID ของ PVC นั้น
ตั้ง node affinity ไป Node เครื่องนี้และ reclaim policy เป็น `Retain`; ไม่แก้ PVC, StatefulSet templates หรือ default StorageClass
PVC ที่ Bound อยู่แล้วจะไม่ถูกแก้ และจะหยุดหากพบ PV ที่ถูกจองไว้เดิมหรือโฟลเดอร์ที่มีอยู่โดยไม่มี PV ที่ตรงกัน
ก่อนสร้างโฟลเดอร์จะตรวจว่าไม่มี symlink; ใช้ `sudo install` เฉพาะการสร้างโฟลเดอร์ใหม่พร้อม ownership ตาม StatefulSet
ไม่ chown/chmod ข้อมูลเดิม ไม่ลบ PVC/PV และไม่ลบ directory เพื่อแก้ปัญหา

ชุดปัจจุบันได้โฟลเดอร์แยก 5 ก้อน:

```text
/var/lib/ida/storage/core/data-core-db-0
/var/lib/ida/storage/core/data-queue-0
/var/lib/ida/storage/bu01/data-bu-db-0
/var/lib/ida/storage/bu02/data-bu-db-0
/var/lib/ida/storage/bu03/data-bu-db-0
```

ความจุ 10Gi/5Gi ใน PV ใช้จับคู่คำขอ PVC ไม่ใช่ disk quota หรือการแบ่ง partition
โฟลเดอร์เหล่านี้ใช้ดิสก์ของเครื่องเดียวกัน ต้องดูพื้นที่ว่างและ backup เอง; ไม่ใช่ HA หรือ storage สำหรับ production
หาก Node/ดิสก์เสีย DB จะเข้าถึงข้อมูลไม่ได้ การเปลี่ยน `KUBE_STORAGE_ROOT` ไม่ย้ายข้อมูลที่ Bound แล้ว
หากขั้นตอนล้มเหลว บางโฟลเดอร์/PV อาจถูกสร้างแล้วและจะถูกเก็บไว้ให้ตรวจ ไม่ล้างข้อมูลอัตโนมัติ
Local PVs สร้างจาก Node/PVC จริงตอนรัน `kube:storage` ไม่อยู่ใน output ของ `kube:render`

`kube:images` อ่าน `ida/*` images จาก Deployment manifests, รัน `npm run docker:build`, บันทึก archive ชั่วคราว
แล้วใช้ `sudo ctr -n k8s.io images import` นำ images เข้า containerd ที่ Kubernetes ใช้ ไม่ใช่เพียง Docker image cache
ไม่ restart/apply Pods; kubelet จะลอง image อีกครั้งตามรอบ retry ถ้า import ล้มเหลวจะเก็บ archive และแสดงคำสั่งให้ดำเนินการต่อ
ต้องใช้ Docker daemon ที่ผู้ใช้เข้าถึงได้, containerd/ctr และสิทธิ์ sudo; ไม่ต้องรัน npm ทั้งคำสั่งด้วย sudo
ยังต้องให้ Node ดาวน์โหลด `postgres:17-bookworm` และ `rabbitmq:4.2.9` ได้สำหรับ DB/Queue

สำหรับสร้างใหม่ตั้งแต่ต้น หลังเตรียม credentials ใน `.env`:

```sh
npm run kube:render
npm run kube:images
npm run kube:secrets
npm run kube:apply
npm run kube:storage
npm run kube:db-info
```

หลังสร้าง Local PV แล้ว Kubernetes อาจ bind PVC และเริ่ม DB/Queue ที่ Pending อยู่โดยอัตโนมัติ
คำสั่งเตรียมไม่รอ readiness หรือทดสอบ SQL ให้รัน `kube:db-info` อีกครั้งเมื่อ Pods พร้อมแล้ว

คัดลอกไฟล์ตัวอย่างจากโฟลเดอร์ `project/` โดยไม่ทับไฟล์ที่มีอยู่:

```sh
cp -n .env.example .env
```

ใช้ `.env` ที่ root ของ `project/` เป็นแหล่ง credential หลักก่อนสร้าง Secrets:

- `CORE_DB_PASSWORD`, `BU01_DB_PASSWORD`–`BU03_DB_PASSWORD`: รหัสผ่าน app user
- `CORE_DB_ADMIN_PASSWORD`, `BU01_DB_ADMIN_PASSWORD`–`BU03_DB_ADMIN_PASSWORD`: รหัสผ่านผู้ดูแล PostgreSQL
  ใช้คนละค่ากับ app user; `kube:secrets` ไม่อ่าน `database.secret.env` ราย BU อีกแล้ว
- `CORE_DB_CONNECTION`: คง Service DNS, database `ida_core` และ username `core_app` ตาม manifests โดยไม่ใส่ Password
- `CORE_QUEUE_CONNECTION`, `BU01_QUEUE_CONNECTION`–`BU03_QUEUE_CONNECTION`: คง broker DNS, port และ vhost ตามตัวอย่าง
  เปลี่ยน credentials ใน URL ก่อนใช้งานจริง

ใช้รหัสผ่านสุ่มคนละชุดทุก BU และทุกบัญชี ไม่ใช้ `REPLACE_ME`
สำหรับ scaffold นี้ใช้ password แบบ hex 64 ตัวอักษร เช่นค่าที่สร้างด้วย `openssl rand -hex 32`
เพื่อไม่ต้อง escape อักขระพิเศษใน connection string; ไฟล์ `.env` และ `*.secret.env` ถูก gitignore ไว้แล้ว
ไม่ต้องคัดลอก password ไปไฟล์ราย BU; `.env.example` ราย BU/Core และ `database.secret.env.example` เป็นตัวอย่างรูปแบบ Secret สำหรับเตรียมเองเท่านั้น
`BUxx_DB_CONNECTION` ใช้กับ Compose เท่านั้น; Kubernetes ใช้ Service `bu-db` ใน namespace ของแต่ละ BU

เมื่อแก้ค่าเรียบร้อย ให้ผู้ใช้ตรวจว่า kubectl context ถูกต้อง แล้วสร้าง/อัปเดต namespaces และ Secrets:

```sh
npm run kube:secrets
```

คำสั่งนี้อ่าน `.env` โดยไม่ execute เป็น shell; environment variables ของ process มีลำดับความสำคัญสูงกว่า `.env`
ตรวจค่าจำเป็นก่อนติดต่อ cluster และไม่ยอมรับ `REPLACE_ME`/`example.invalid`
รายชื่อ BU อ่านจาก Namespace manifests ที่ `kube:apply` ใช้; เมื่อเพิ่ม BU ต้องเพิ่ม config สาขาและค่า env ของ BU นั้น
ส่ง Secrets ผ่าน stdin โดยไม่พิมพ์ค่า credential และไม่ restart workloads
จากนั้นใช้ `npm run kube:apply` เพื่อ deploy Workers และ DB ทั้ง 3 คู่พร้อม Core API/Web, Core DB และ RabbitMQ
หากแก้รหัสผ่านบน DB ที่มีข้อมูลแล้ว ต้องจัดการ password rotation ใน DB และโหลด config ใหม่เอง ไม่ใช่เพียงแก้ Secret
เช่นเดียวกับ RabbitMQ: boot import ไม่ overwrite บัญชี/objects ที่มีอยู่แล้ว
การเปลี่ยน credentials ภายหลังต้องจัดการใน broker ให้ตรงกับ Secrets และให้ client โหลดค่าใหม่เอง

ลบทรัพยากรของชุดนี้จาก cluster เมื่อไม่ต้องการใช้งานแล้ว:

```sh
npm run kube:purge
```

คำสั่งนี้แสดง kubectl context, ค้นหา namespaces ที่มี label `ida.io/role` ใน cluster
ตรวจ role และ BU label ก่อนลบ แล้วลบ namespaces ที่พบพร้อมทุกอย่างภายใน รวมทั้ง Secrets, Workers, DB, RabbitMQ และ PVC
จึงครอบคลุม BU ที่เพิ่มภายหลังหรือ BU เก่าที่ยังอยู่ใน cluster แม้ไม่อยู่ใน manifests ปัจจุบัน
**ข้อมูลใน PVC อาจสูญหายถาวร**; PV/ที่เก็บข้อมูลจริงจะถูกจัดการตาม reclaim policy ของแต่ละ PV
Local PV ที่สร้างด้วย `kube:storage` ใช้ `Retain`: purge ไม่ลบ PV หรือโฟลเดอร์ `/var/lib/ida/storage`
เมื่อ PVC ถูกลบ PV จะเป็น Released และยังจอง UID ของ PVC เดิมไว้ จึงต้องตรวจข้อมูล/วางแผน recovery ก่อนใช้กับ PVC ใหม่
`kube:storage` ไม่ล้าง claimRef หรือย้ายข้อมูลเก่ามาผูกกับ PVC ใหม่ให้อัตโนมัติ
หาก namespace มีทรัพยากรอื่นเพิ่มเติม ทรัพยากรเหล่านั้นจะถูกลบด้วย; `.env` และไฟล์ในเครื่องไม่ถูกลบ
ตรวจ `kubectl config current-context` ให้ตรงกับ cluster ที่ต้องการก่อนรันคำสั่งนี้

### ข้อจำกัดและการเก็บข้อมูล

- Default config เหลือ BU01–BU03; การเอา BU04–BU13 ออกจากไฟล์ไม่ได้ลบ workloads/DB/PVC ที่เคย deploy ไว้ใน cluster
  ไม่มีการรัน prune, delete หรือหยุด containers อัตโนมัติ
- DB ใช้ PostgreSQL 17 และ initialize database/user เฉพาะ volume ว่าง การแก้ Secret ไม่เปลี่ยนรหัสผ่านใน DB เดิม
  ต้องจัดการ password rotation ใน PostgreSQL ให้ตรงกัน ไม่ลบ PVC เพื่อเปลี่ยนรหัสผ่าน
- BU config ใช้ app user `bu01user`–`bu03user` แล้ว หาก DB เดิม initialize ด้วย `bu01_app`–`bu03_app`
  ต้องปรับ role/สิทธิ์ของ DB เดิมให้ตรงกันก่อนนำ config นี้ไปใช้ ไม่ได้ rename user ที่มีอยู่ให้อัตโนมัติ
- PVC เก็บข้อมูลข้ามการสร้าง Pod ใหม่ และ StatefulSet ใช้พฤติกรรมเริ่มต้นที่ไม่ลบ PVC เมื่อ scale down/ลบ StatefulSet
  แต่การลบ namespace หรือ PVC อาจทำให้ข้อมูลสูญหายตาม reclaim policy ของ storage
- DB และ RabbitMQ เป็น single instance สำหรับทดลอง ยังไม่มี HA, backup/restore, TLS หรือ schema migrations
  ห้ามเพิ่ม replicas ของ DB เพื่อทำ replication โดยตรง ต้องวางระบบ replication/DB operator ก่อน
  RabbitMQ ก็ยังไม่ได้ตั้ง clustering; ห้ามเพิ่ม replicas แล้วถือว่าเป็น broker cluster
- หากขาด Secret/image หรือ storage ยังไม่พร้อม Pod อาจไม่เริ่มทำงาน; แม้ Pods เริ่มได้ Worker ก็ยังเป็น scaffold
  ยังไม่รับ Queue หรือ query DB จริง ต้องเพิ่ม client และ logic ของแอปต่อ

Core API/Web ใช้ ClusterIP; ยังไม่มี public Ingress/TLS หรือ authentication
การเปิดให้ Mobile/Web ภายนอกเข้าถึงต้องวาง gateway/Ingress และสิทธิ์ก่อน

## Claude / Codex

มีเฉพาะ Claude/Codex configuration และ Nx skills:
`nx-workspace`, `nx-generate`, `nx-run-tasks`, `nx-plugins`, `nx-import`
ยังไม่เพิ่ม skills ของ .NET, Flutter หรือ Next.js และไม่มี Nx Cloud/CI monitor อัตโนมัติ

## References

- [Nx custom commands](https://nx.dev/docs/reference/nx/executors)
- [Kubernetes Kustomize](https://kubernetes.io/docs/tasks/manage-kubernetes-objects/kustomization/)
- [Kubernetes StatefulSets และ persistent storage](https://kubernetes.io/docs/concepts/workloads/controllers/statefulset/)
- [Kubernetes Local volumes](https://kubernetes.io/docs/concepts/storage/volumes/#local)
- [Kubernetes การจอง PV ให้ PVC](https://kubernetes.io/docs/concepts/storage/persistent-volumes/#reserving-a-persistentvolume)
- [PostgreSQL official image และ database initialization](https://hub.docker.com/_/postgres)
- [RabbitMQ definitions import](https://www.rabbitmq.com/docs/definitions)
- [RabbitMQ password hashing](https://www.rabbitmq.com/docs/passwords)
- [RabbitMQ access control](https://www.rabbitmq.com/docs/access-control)
- [Docker Compose environment interpolation](https://docs.docker.com/compose/how-tos/environment-variables/variable-interpolation/)
- [Node.js environment file parsing](https://nodejs.org/api/environment_variables.html)
- [Kubernetes multi-tenancy](https://kubernetes.io/docs/concepts/security/multi-tenancy/)
- [Next.js standalone output](https://nextjs.org/docs/app/api-reference/config/next-config-js/output)
- [.NET watch](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-watch)
- [Flutter build modes](https://docs.flutter.dev/testing/build-modes)
- [Flutter iOS setup](https://docs.flutter.dev/platform-integration/ios/setup)
