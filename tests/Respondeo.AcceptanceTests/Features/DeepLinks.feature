Feature: Deep links
  As a visitor arriving from a search engine or a shared link
  I want a deep link to load its own page directly
  So that I land on the content I was sent to

  Scenario Outline: A deep link is served with HTTP 200 and its own canonical URL
	When I request "<path>" directly
	Then the response status should be 200
	And the canonical URL should end with "<path>"

	Examples:
	  | path              |
	  | discover          |
	  | discover/saints   |
	  | discover/prayers  |
	  | summa             |
	  | bible             |
	  | bible/john/3      |
	  | discover/prayers/act-of-contrition                     |
	  | discover/saints/augustine-of-hippo                     |
	  | discover/articles/sacrament-anointing                  |
	  | discover/miracles/bernadette-soubirous-incorrupt-body  |

  Scenario Outline: An unknown address is served with HTTP 404
	When I request "<path>" directly
	Then the response status should be 404

	Examples:
	  | path                                |
	  | this-page-does-not-exist            |
	  | discover/prayers/no-such-prayer     |
	  | discover/saints/no-such-saint       |
	  | bible/no-such-book/1                |
