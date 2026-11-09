Feature: Smoke
  As the maintainer restructuring the app
  I want every page to load cleanly and keep its styling and settings
  So that refactors cannot silently break the site

  Scenario Outline: A page loads without errors or failing requests
	Given I am watching for page errors
	When I visit "<path>"
	Then no page errors should have occurred

	Examples:
	  | path                               |
	  |                                    |
	  | why-god                            |
	  | thanks                             |
	  | summa                              |
	  | summa/prima-q001                   |
	  | bible                              |
	  | bible/drb/john                     |
	  | bible/drb/john/3                   |
	  | discover                           |
	  | discover/saints                    |
	  | discover/saints/augustine-of-hippo |
	  | discover/prayers                   |
	  | discover/prayers/hail-mary         |
	  | discover/miracles                  |
	  | discover/articles                  |
	  | discover/hours                     |
	  | discover/hours/morning             |
	  | discover/devotions                 |

  Scenario: Component-scoped styles are applied
	Given I open the start page
	When I open the quick menu
	Then the largest text size button should use its scoped style

  Scenario: The chosen text size survives a reload
	Given I open the start page
	When I open the quick menu
	And I choose the "large" text size
	And I reload the page
	Then the text size should be "large"

  Scenario: An unknown address shows the not found page
	Given I open the "this-page-does-not-exist" path directly
	Then I should see the not found page

  Scenario: The thanks page renders
	Given I open the "thanks" path directly
	Then I should see the heading "With Gratitude"

  Scenario: The service worker files are published
	Then the service worker files should be published
