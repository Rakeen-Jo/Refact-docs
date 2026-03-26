# 10. Watchdog / Recovery / Fault Handling

## 목표
- 영구 정지 대신 자동 복구/리셋 유도
- 리셋 후 원인 역추적 가능하게 태그/카운터 남김

## 계층별 방어
1. **Task 레벨**
   - loop liveness 확인
   - period overrun 감시
2. **Driver 레벨(UART/DMA)**
   - `XC330_T181_ErrorRecover`로 에러 플래그 정리 + DMA 재암
   - RX deadlock watchdog으로 비정상 상태 강제 해소
3. **System 레벨**
   - IWDG/Watchdog로 최종 회복
   - crash tag/monitor로 사후 분석

## 관찰 지표
- reset cause
- crash tag(last/prev)
- per-port `UErr/UORE/dead/Stmo`

## 운영 결론
- fault handling은 "에러 0"보다 "에러 발생 시 빠른 회복"이 중요
- deadlock 카운터가 일정 임계 이상이면
  - 타이밍 튜닝만이 아니라 배선/트랜시버/전원까지 점검 필요

## 개선 TODO
- [ ] deadlock 개입 시점의 추가 로그(포트/상태 스냅샷)
- [ ] 복구 후 첫 정상 프레임까지 latency 계측
- [ ] monitor에 recovery rate(분당) 지표 추가
