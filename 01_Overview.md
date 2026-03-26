# 01. Overview

## 프로젝트 목적
- 3ms 제어 루프를 안정적으로 유지
- CAN/TCP 경로가 Motor loop를 굶기지 않도록 분리
- Sensor/Motor UART DMA 경로의 timeout·deadlock·error recover 체계화

## 최종 아키텍처 한 줄 요약
- **Motor Task(3ms 고정)**: 내부 상태 갱신/제어 적용 중심
- **CAN/TCP Task**: 요청 기반 응답 + 제어 명령 반영
- **UART DMA 경로**: 에러/데드락 복구 루틴 + 모니터 카운터로 운영

## 범위
- Firmware 비교
  - Before: `work/02_V5B2_SENSE`
  - After: `work/02_V5B2_SENSE_Refact_2ea0133`
- PC: `work/V5B2_Software`
- Sensor MCU
  - ESP-IDF: `work/Finger_ESP32`
  - Arduino legacy: `work/02_ESP32C3_DEV_madi_20251113`, `work/02_ESP32C3_DEV_Robotfinger_B2_20260123`

## 이번 분석 핵심 결론
1. 초기 대형 문제는 연산량보다 **스케줄링/블로킹/IRQ 우선순위** 이슈가 지배적
2. CAN 경로는 IRQ priority와 TX blocking 정책 영향이 큼
3. Sensor 경로는 포트별(U1/U3/U4/U6) 타이밍 편차와 UART error recover 품질이 실효 성능을 좌우
4. ESP-IDF 센서 펌웨어는 ISR 수신/응답은 동작하나, half-duplex 타이밍 안정화(지연/late-cut/복구)가 핵심

## 문서 사용 방법
- 본 문서는 Notion에 페이지 단위로 붙여넣기 전제
- 상세 커밋/로그 근거는 `99_Appendix_Diff_and_Commits.md`와 DB에서 관리
