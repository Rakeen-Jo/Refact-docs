# 13. Function-Level Diff Map (핵심)

> 목적: "무엇이 어디로 이동/분리/신설됐는지"를 함수 단위로 빠르게 추적

## A. Core main.c 역할 재정의

### Before (`02_V5B2_SENSE/Core/Src/main.c`)
- 단일 파일에 시스템 초기화 + 태스크 + 통신/모터 보조 로직이 상대적으로 밀집
- 대표 함수군
  - init: `SystemClock_Config`, `MX_*_Init`
  - task entry: `Start_Task_*`
  - 통신/보조: `CAN_SendData`, `SendFingerPosition` 등 일부 혼재

### After (`02_V5B2_SENSE_Refact_2ea0133/Core/Src/main.c`)
- main.c는 **태스크 orchestration + 시스템 초기화** 중심
- 대표 함수군
  - task entry: `vTask_System`, `vTask_CANComm`, `vTask_MotorCtrl`, `vTask_TCPServer`, `vTask_TermPrint`
  - 시스템: `MX_IWDG_Init`, `MX_RTC_Init`, crash log 함수
- 통신/모터 세부 로직은 `V5B2/Src/*`로 분리

## B. UART DMA 엔진 확장

### Before (`Core/Src/xc330-t181-dma.c`)
- DMA queue / parse / timeout 기본 골격 존재
- 대표 함수
  - `XC330_T181_Dma_Init`
  - `XC330_T181_ProcessTxQueue_Dma`
  - `XC330_T181_TxCpltCallback_Dma`
  - `XC330_T181_OnRxEvent`
  - `XC330_T181_ParseStep`
  - `XC330_T181_Timeout_Handler`

### After (`V5B2/Src/motor_xc330_dma.c`)
- 기존 골격 + **복구/진단/센서 라운드 관리** 대폭 강화
- 신설/확장 대표 함수
  - recover/deadlock: `XC330_T181_ErrorRecover`, `port_rx_force_reset`
  - done buffer: `done_*_push/pop_*`
  - sensor round: `queue_sensor_read_common`, `sensor_round_abort`, `XC330_IssueSensorReadNow`
  - 진단: `XC330_GetPortStat` 계열(모니터 필드 공급)

## C. motor_ctrl 분리/오케스트레이션

### After (`V5B2/Src/motor_ctrl.c`)
- 정책 함수군
  - `motor_service_cycle`
  - `motor_port_service_u1/u3/u4/u6`
  - `motor_on_write_txcplt`
  - `motor_update_angles`
- 계측/윈도우 함수군
  - `motor_cycle_*`

> 포인트: per-port 서비스 정책과 loop-level orchestration이 분리되어, 병목/오류를 포트 단위로 추적 가능.

## D. CAN/TCP 도메인 분리

### CAN (`V5B2/Src/can_comm.c`)
- ISR + task 처리 분리
- 진단 창 함수군: `can_diag_*`
- 서비스 루프: `can_task_service`

### TCP (`V5B2/Src/ethernet.c`)
- 수신 파서/응답/모니터 함수군 분리
- `tcp_diag_*`, `tcp_handle_*`, `TcpTask`

## E. 문서 작성 시 함수 추적 템플릿
| 도메인 | Before 함수 | After 함수 | 변경 이유 | 성능/안정성 영향 |
|---|---|---|---|---|
| UART DMA | `XC330_T181_Timeout_Handler` | `XC330_T181_Timeout_Handler` + deadlock/recover 함수군 | timeout 단독으로 못 잡는 케이스 대응 | dead/cut/UErr 추적 가능 |
| Motor loop | `Start_Task_Motor_Ctrl` | `vTask_MotorCtrl` + `motor_service_cycle` | 역할 분리 | 병목 지점 분리 용이 |
| CAN | main 혼재 로직 | `can_task_service` 중심 | 도메인 분리 | starvation 위험 관리 |
| TCP | main/LWIP 혼재 | `tcp_handle_*` | 파서/응답 구조화 | 유지보수 용이 |
