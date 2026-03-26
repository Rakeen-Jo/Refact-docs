# 02. Architecture Before / After

## Before (02_V5B2_SENSE)
- 모터/센서/CAN/TCP 경로가 코드 레벨에서 강결합되어 있고, 장애 원인 분리가 어려움
- UART DMA/에러 복구/모니터링 카운터가 지금보다 단순해, "왜 멈췄는지" 추적 난이도 높음
- task/IRQ 경계에서 정책(우선순위, blocking 금지, timeout)이 문서화/계측 기준으로 고정되지 않음

## After (02_V5B2_SENSE_Refact_2ea0133)
- 모듈 책임이 명확해짐
  - `motor_ctrl.c`: 루프 정책/포트 스케줄/상위 제어 로직
  - `motor_xc330_dma.c`: UART DMA TX/RX, Parse, timeout, recover, 포트 통계
  - `can_comm.c`, `ethernet.c`: 통신 도메인 분리
  - `term_print.c`, `V5B2_main.c`: 출력/모니터/운영 보조
- 운영 관점 계측이 대폭 강화
  - 포트별 `SRtx/SRdone/Stmo/cut/dead/UErr/Sid/Slen` 등
  - cycle 지표 `LoopExec/MotorE2E/SensorE2E`
- 장애 대응 경로가 명시적
  - UART/DMA 에러 복구
  - RX deadlock watchdog
  - timeout/late-cut/abort 정책

## Block Diagram (텍스트)
### Before
PC(CAN/TCP) ↔ FW(main) ↔ Motor/Sensor UART

### After
PC(CAN/TCP)
→ `can_comm.c` / `ethernet.c`
→ `motor_ctrl.c` (3ms 루프 오케스트레이션)
→ `motor_xc330_dma.c` (DMA queue + parse + timeout + recover)
→ UART1/3/4/6 (Motor/Sensor)

## 주요 차이 표
| 항목 | Before | After | 효과 |
|---|---|---|---|
| 모듈 경계 | 혼재 | 도메인별 분리 | 분석/유지보수 용이 |
| 계측 | 제한적 | 포트/루프 상세 계측 | 원인 추적 속도 향상 |
| UART 에러 대응 | 단편적 | recover + deadlock watchdog | 장기 멈춤 방지 |
| 통신 처리 | 경합 가능 | 요청 기반 중심으로 재정리 | Motor loop 간섭 감소 |
| 운영 문서화 | 약함 | monitor 필드 기준 운영 가능 | 현장 튜닝 용이 |

## 이번 프로젝트에서 확인된 핵심 교훈
1. 병목은 연산량보다 **스케줄/블로킹/IRQ 정책 위반**에서 주로 발생
2. "고장 후 원인 추적"을 줄이려면 처음부터 계측 항목을 설계해야 함
3. half-duplex UART는 기능 구현보다 타이밍/복구 정책이 안정성을 결정함
