# ArbitrageBettingSln

ABShared - Содержит типы, для взаимодействия между ABClient и ABServer.
            Есть класс ProjectVersion, который содержит текущую версию проекта. Что бы срабатывало обновлене
            нужно перед каждым релизом обновлять 
ABClient - проект клиенского приложения. Взаимодействие с сайтами БК происходит через выполнение JS. 
ABServer  - проект серверного приложения. 
DeployLeader - Проект для развертывания клиента. Лучше использовать его(чистить все настройки/куки/
               проверят версию/заливает на хостинг). Нужно использовать Debug конфигурацию. 
EditMaps -  Редактор, для маппинга названий команд между БК. к нему также относяться проекты
            StaticData, StaticData.Shared
ProxyChecker - чекер прокси
Updater - апдейтер клиентского приложения.
SportBase.Editor - новый проект для замены EditMaps.
