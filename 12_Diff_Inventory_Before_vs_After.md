# 12. Diff Inventory (Before vs Refact)

기준:
- Before: `02_V5B2_SENSE`
- After: `02_V5B2_SENSE_Refact_2ea0133`

## 구조적 변화

### A) Core에 있던 도메인 코드의 V5B2 모듈화
Before에서 `Core/Inc|Src`에 있던 일부 기능이 After에서 `V5B2/`로 이동/분리됨.

예)
- `Core/Src/xc330-t181*.c` -> `V5B2/Src/motor_xc330*.c`
- `Core/Src/Interface.c` -> 도메인별 모듈(`can_comm.c`, `ethernet.c`, `term_print.c`, `V5B2_main.c`)

### B) 문서/운영 산출물 강화
- Before의 단일 문서/엑셀 중심에서
- After는 drawio/분석 md/비교 문서가 다수 추가

### C) 드라이버/시스템 범위 확장
- After에 IWDG/RTC 관련 HAL 파일 포함
- fault/recovery 운영성 강화 방향

## 핵심 변경 파일 그룹

### 1) Scheduler/Startup/IRQ
- `Core/Src/main.c`
- `Core/Src/stm32f4xx_it.c`
- `Core/Src/stm32f4xx_hal_msp.c`
- `Core/Inc/main.h`

### 2) Motor/Sensor UART DMA
- `V5B2/Src/motor_ctrl.c`
- `V5B2/Src/motor_xc330_dma.c`
- `V5B2/Src/motor_xc330.c`
- `V5B2/Inc/motor_*.h`

### 3) Communication
- `V5B2/Src/can_comm.c`
- `V5B2/Src/ethernet.c`
- `LWIP/Target/*.c`

### 4) Debug/Monitoring
- `V5B2/Src/term_print.c`
- `V5B2/Src/V5B2_main.c`

## 변경 분류 (권장 태깅)
- [ARCH] 구조 변경
- [RT] 실시간/스케줄
- [DMA] UART DMA
- [COM] CAN/TCP
- [OBS] 모니터/계측
- [REC] 복구/내고장성

## 실제 작성 가이드
각 파일별로 아래 5줄만 채우면 문서 완성도가 급상승:
1. 왜 바꿨는지
2. 무엇을 바꿨는지
3. 실시간 영향(지연/주기/블로킹)
4. 장애 대응 영향(복구/카운터)
5. 검증 근거(로그/그래프/테스트)
