# 05. Motor Timing Stabilization

## 목표
- Motor loop 3ms 주기 유지
- loop jitter/overrun 최소화
- 통신(CAN/TCP)과 상호 starvation 방지

## 실제로 했던 작업 축
1. **init 구간 블로킹 제거**
   - `HAL_Delay` 기반 대기 제거/축소(스케줄러 양보형으로 전환)
2. **loop 계측 삽입**
   - `LoopExec`, `MotorE2E`, `MotorFromLoop` 추적
3. **서비스 경로 분해 실험**
   - drain/write/read 경로 분리 테스트
   - bypass 플래그로 병목 범위 축소
4. **통신 경로 blocking 제거**
   - CAN TX mailbox 대기 정책 개선(장시간 busy-wait 제거)

## 대표 지표 해석
- `LoopExec(us)` : 모터 태스크 본문 실행시간
- `MotorE2E(us)` : 모터 트랜잭션 왕복(요청→완료) 창
- `MotorFromLoop(us)` : loop 시작 기준 모터 완료 시점

## 안정화 후 기대 형태
- `LoopExec` max가 3ms budget 내 유지
- `MotorE2E` max가 안정 범위에서 수렴
- 통신 연결/부하 변화가 Motor loop를 깨지 않음

## 운영 체크리스트
- [ ] 모드 전환(CAN/TCP) 시 loop 지표 급변 없는지
- [ ] ISR/API 정책 위반으로 assert/reset 없는지
- [ ] 모니터 interval 기준 `max`가 장시간 드리프트하지 않는지

## 남은 리스크
- 포트 편차(U1 vs U3/U4/U6)가 loop tail latency에 간헐 영향 가능
- 센서 지연 튜닝값(REPLY_DELAY/LATE_CUT)이 과도하면 SRdone 손실 가능
