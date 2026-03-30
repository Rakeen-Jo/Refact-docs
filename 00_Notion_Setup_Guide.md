# V5B2 Refact Notion 세팅 가이드

아래 파일들을 Notion에 **각각 새 페이지로 붙여넣기**하면 됩니다.

## 권장 순서
1. `01_Overview.md`
2. `02_Architecture_Before_After.md`
3. `03_Task_Scheduler.md`
4. `04_UART_DMA.md`
5. `05_Motor_Timing_Stabilization.md`
6. `06_Sensor_Timing_Stabilization.md`
7. `07_CAN_Implementation.md`
8. `08_TCP_Implementation_and_Protocol.md`
9. `09_Debug_Menu_TermPrint_Monitor.md`
10. `10_Watchdog_Recovery_Fault_Handling.md`
11. `11_Bug_Timeline_and_Fix_Map.md`
12. `12_Diff_Inventory_Before_vs_After.md`
13. `13_Function_Level_Diff_Map.md`
14. `14_Validation_and_Release_Checklist.md`
15. `15_IAP_Menu_Protocol_and_StateMachine.md`
16. `99_Appendix_Diff_and_Commits.md`

## DB(데이터베이스) 3개
- Change Log DB
- Issue/Fix Timeline DB
- Protocol/Spec DB

DB 컬럼 템플릿은 `DB_Templates.md`를 그대로 사용.

## 작성 팁
- 각 섹션의 `[TODO]`를 채우는 방식으로 작성
- 코드 경로는 상대경로보다 repo 루트 기준 절대경로를 같이 명시
- 커밋 해시는 짧은 해시 + 링크 둘 다 적기
