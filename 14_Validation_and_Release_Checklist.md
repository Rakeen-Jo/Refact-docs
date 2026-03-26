# 14. Validation & Release Checklist

## 1) Real-time / Task
- [ ] Motor loop 3ms 주기 유지 (`LoopExec` max 기준 통과)
- [ ] CAN 연결/해제 반복 시 System/Term task liveness 유지
- [ ] TCP 연결/해제 반복 시 starvation 없음

## 2) UART DMA / Sensor
- [ ] 포트별 `SRtx≈SRdone` 수렴
- [ ] `Stmo/cut/dead` 증가율이 허용 범위 이내
- [ ] `UErr/UORE` 급증 없음
- [ ] U1/U3/U4/U6 교차 테스트 완료

## 3) CAN
- [ ] IRQ priority 정책 검증(`MAX_SYSCALL` 경계 준수)
- [ ] mailbox full 시 장시간 busy-wait 없음
- [ ] 지터 측정(FW vs PC 타임스탬프 분리) 완료

## 4) TCP
- [ ] req->resp avg/p99/max 측정
- [ ] reconnect 안정성(장시간 soak) 확인
- [ ] protocol version/field 호환성 체크

## 5) Recovery / WDT
- [ ] error recover 루프에서 영구 stuck 없음
- [ ] deadlock watchdog 개입 시 자동 회복 확인
- [ ] reset cause/crash tag 기록 확인

## 6) 문서/릴리즈
- [ ] Change Log DB 업데이트
- [ ] Issue/Fix Timeline DB 업데이트
- [ ] Protocol/Spec DB 업데이트
- [ ] 최종 보고서(HTML/PDF/Notion) 동기화

---

## 배포 전 승인 템플릿
- Build hash:
- FW version:
- PC SW version:
- Sensor FW version:
- 승인자:
- 승인 일시:
- 비고:
