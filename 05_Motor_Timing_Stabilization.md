# 05. Motor Timing Stabilization

## 목표
- 3ms 루프 유지
- 제어 데이터 반영 지연 최소화

## 주요 작업
- [TODO] HAL_Delay -> osDelay 전환
- [TODO] loop 계측 추가
- [TODO] write/read 분리
- [TODO] overrun 대응

## 지표
- LoopExec(us): last/min/max
- MotorE2E(us): last/min/max
- MotorFromLoop(us): last/min/max

## 결론
- [TODO] 병목 원인
- [TODO] 최종 안정화 전략
