# V5B2 Communication Protocol v1.2 (from firmware commit 0f89e88)

## 1) 네트워크 기본값

| 항목 | 값 | 출처 |
|---|---:|---|
| IP | 192.168.0.50 | `env_flash.c` (`MY_IP_ADDRESS`) |
| Netmask | 255.255.255.0 | `env_flash.c` (`MY_NET_MASK`) |
| Gateway | 192.168.0.1 | `env_flash.c` (`MY_GATEWAY_IP`) |
| UDP Port | 7000 | `env_flash.c` (`MY_UDP_PORT`) |
| TCP Port | 23 | `env_flash.c` (`MY_TCP_PORT`) |

> 실제 운용값은 ENV 파라미터 저장값을 따름.

---

## 2) 공통 패킷 헤더 (TCP/UDP 공통)

`V5B2/Inc/tcp_hand_proto.h`

| 필드 | 타입 | 크기 | 설명 |
|---|---|---:|---|
| magic | `uint32` | 4 | 고정 `0x32423556` (`"V5B2"`) |
| version | `uint8` | 1 | 현재 `0x01` |
| type | `uint8` | 1 | 메시지 타입 |
| length | `uint16` | 2 | payload 길이 |
| seq | `uint32` | 4 | 전송 시퀀스 |
| timestamp_ms | `uint32` | 4 | 송신 tick(ms) |

- 헤더 크기: **16 bytes**
- 구조체 packed 사용 (`#pragma pack(push,1)`)
- STM32F4 기준 **리틀엔디언**

---

## 3) 메시지 타입 표

| Type | 이름 | 방향 | Payload |
|---:|---|---|---|
| `0x01` | HELLO_REQ | PC → FW (TCP) | 없음 |
| `0x02` | HELLO_RESP | FW → PC (TCP) | 7 bytes (`00 06 00 06 00 00 01`) |
| `0x10` | TELEMETRY_RAW | FW → PC (UDP 권장) | TelemetryRaw(164B) |
| `0x11` | TORQUE_CMD | PC → FW (**UDP 전용**) | TorqueCmd(32B) |
| `0x20` | TTL_REQ | PC → FW (TCP) | 없음 |
| `0x21` | TTL_RESP | FW → PC (TCP) | 8B (motor/sensor fail bitmask) |
| `0x22` | SN_READ_REQ | PC → FW (TCP) | 없음 |
| `0x23` | SN_RESP | FW → PC (TCP) | 8B SN |
| `0x24` | SN_WRITE_REQ | PC → FW (TCP) | 8B SN |
| `0x25` | JAC_REQ | PC → FW (TCP) | 없음 |
| `0x26` | CAL_DONE | FW → PC (TCP) | 없음 |
| `0x27` | LED_ALT_REQ | PC → FW (TCP) | 없음 |
| `0x28` | LED_BLUE_REQ | PC → FW (TCP) | 없음 |
| `0x29` | NET_CONFIG_REQ | PC → FW (TCP) | 없음 |
| `0x2A` | NET_CONFIG_RESP | FW → PC (TCP) | NetConfig(14B) |
| `0x2B` | NET_CONFIG_WRITE_REQ | PC → FW (TCP) | NetConfig(14B) |
| `0x30` | MOTOR_ERROR | FW → PC (TCP) | MotorError(2B) |

---

## 4) Payload 상세

### 4.1 TELEMETRY_RAW (`0x10`, 164 bytes)

| 필드 | 타입 | 개수 | 크기 |
|---|---|---:|---:|
| sample_seq | `uint32` | 1 | 4 |
| position | `int16` | 16 | 32 |
| current | `int16` | 16 | 32 |
| sensor | `uint16` | 16 | 32 |
| velocity | `int32` | 16 | 64 |

총 payload = **164B**, packet = **180B(16+164)**

### 4.2 TORQUE_CMD (`0x11`, 32 bytes)

| 필드 | 타입 | 개수 | 크기 |
|---|---|---:|---:|
| torque_mA | `int16` | 16 | 32 |

총 payload = **32B**, packet = **48B(16+32)**

주의:
- 최신 FW에서 `TORQUE_CMD`는 **UDP로만 처리**.
- 같은 타입을 TCP로 보내면 FW는 무시함(하위호환용).

### 4.3 TTL_RESP (`0x21`, 8 bytes)

| 바이트 | 의미 |
|---|---|
| 0~3 | `motor_mask` (bit i = joint i fault) |
| 4~7 | `sensor_mask` (bit i = sensor i fault) |

### 4.4 SN_RESP / SN_WRITE_REQ (8 bytes)
- 시리얼 번호 8바이트 raw.

### 4.5 NET_CONFIG (14 bytes)

| 필드 | 타입 | 크기 | 예시 |
|---|---|---:|---|
| ip | `uint32` | 4 | `0xC0A80032` (=192.168.0.50) |
| mask | `uint32` | 4 | `0xFFFFFF00` |
| gw | `uint32` | 4 | `0xC0A80001` |
| tcp_port | `uint16` | 2 | `23` |

### 4.6 MOTOR_ERROR (`0x30`, 2 bytes)

| 필드 | 타입 | 설명 |
|---|---|---|
| joint_index_1based | `uint8` | 1~16 |
| error_status | `uint8` | 모터 에러 플래그 |

---

## 5) UDP/TCP 운용 규칙 (중요)

1. **TCP connect 후 HELLO_REQ(0x01) 전송**
   - FW가 HELLO_RESP를 보냄
   - 이때 스트림 활성 플래그가 켜짐

2. **UDP peer IP는 TCP accept 시점의 remote IP를 사용**
   - UDP destination port는 `MY_UDP_PORT`

3. **TORQUE_CMD는 UDP로 주기 송신 권장(3ms)**
   - FW에서 1초(`TCP_TORQUE_MISS_STOP_COUNT`) 무수신 시
     - stream stop
     - torque zero 강제

4. **Telemetry는 UDP 3ms cadence**
   - UdpTxTask에서 `TELEMETRY_RAW(0x10)` 송신

5. TCP는 저빈도 명령/설정/응답용
   - SN, TTL, NET_CONFIG, calibration, error report

---

## 6) PC 구현 체크리스트

- [ ] 모든 패킷 헤더 `magic/version/length` 검증
- [ ] 리틀엔디언 파싱 반영
- [ ] TCP 접속 직후 HELLO_REQ 송신
- [ ] UDP TORQUE_CMD 주기 송신 + 끊김 감지
- [ ] UDP TELEMETRY drop 허용(p99/max 지연 모니터링)
- [ ] NET_CONFIG_WRITE_REQ 후 응답 에코 확인

---

## 7) 코드 기준
- FW repo: `02_V5B2_SENSE_Refact_2ea0133`
- 기준 커밋: `0f89e88` (origin/main)
- 주요 파일:
  - `V5B2/Inc/tcp_hand_proto.h`
  - `V5B2/Src/ethernet.c`
  - `V5B2/Src/env_flash.c`
  - `V5B2/Inc/parms.h`
