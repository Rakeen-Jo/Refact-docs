# 04. UART/DMA 구현 변경

## 대상
- Motor/Sensor UART: USART1/3/UART4/USART6 (2Mbps DMA)
- 포트별 모니터링: pending/timeout/recover/deadlock/cut

## Refact 핵심 포인트
- TX queue 기반 비동기 송신 (`XC330_T181_ProcessTxQueue_Dma`)
- RX idle/event 기반 누적 파싱
- UART/DMA 에러 복구 루틴 표준화 (`XC330_T181_ErrorRecover`)
- deadlock watchdog 추가 (`rx_started=1 && rx_tmo.en=0` 감지)

## 주요 상태 카운터 (모니터 필드)
- `UErr/UORE`: UART 에러 복구 / ORE 복구 횟수
- `Mtmo/Stmo`: motor/sensor timeout 누적
- `cut`: 센서 라운드 중단 횟수
- `dead`: RX deadlock watchdog 개입 횟수
- `Sid/Slen/Scrc`: 마지막 sensor ID/길이/CRC fail

## 실무 해석 규칙
- 절대값보다 **증가량(Δ/interval)** 으로 품질 판단
- `SRtx≈SRdone` + `Stmo/cut/dead` 정체면 안정
- `UErr` 급증 + `dead` 동반 증가 시 half-duplex 타이밍/라인 이슈 우선 점검

## 추천 보강
- 모니터에 누적값 + interval delta 동시 출력
- 포트별 임계치 초과 시 즉시 경고 출력
